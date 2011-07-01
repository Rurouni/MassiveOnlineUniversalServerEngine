using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NLog;
using RakNetWrapper;
using System.Threading.Tasks;

namespace MOUSE.Core
{
    public enum NodeType : byte
    {
        Server,
        Client,
        Master
    }

    public class PendingConnection
    {
        public readonly TaskCompletionSource<NodeProxy> TCS;
        public readonly DateTime StartTime;
        public readonly IPEndPoint Target;

        public PendingConnection(IPEndPoint target)
        {
            TCS = new TaskCompletionSource<NodeProxy>();
            StartTime = DateTime.Now;
            Target = target;
        }
    }

    public class PendingOperation
    {
        public readonly TaskCompletionSource<object> TCS; //uses object because only generic TaskCompletionSource<> version exists
        public readonly DateTime StartTime;
        public readonly uint RequestId;
        public readonly NodeEntityProxy Caller;

        public PendingOperation(uint requestId, NodeEntityProxy caller)
        {
            TCS = new TaskCompletionSource<object>();
            StartTime = DateTime.Now;
            RequestId = requestId;
            Caller = caller;
        }
    }

    public class NodeProxy
    {
        public readonly NetId NetId;
        public readonly NodeDescription Description;
        public readonly Node Owner;

        public NodeProxy(NetId netId, NodeDescription description, Node owner)
        {
            NetId = netId;
            Description = description;
            Owner = owner;
        }

        public void Send(Message msg)
        {
            Owner.Send(NetId, msg);
        }

        public override string ToString()
        {
            return Description.ToString();
        }
    }

    /// <summary>
    /// is not thread safe
    /// </summary>
    public class Node
    {
        private const int MaxMessagesPerTick = 100000;

        public Logger Log = LogManager.GetCurrentClassLogger();

        public readonly NodeType NType;
        private readonly IDomain _domain;
        private NodeDescription _selfDescription;
        private readonly IPEndPoint _masterNodeEndPoint;
        private readonly IPEndPoint _selfEndpoint;
        private readonly NativeReader _reader = new NativeReader();
        private readonly NativeWriter _writer = new NativeWriter();
        private readonly RakPeerInterface _netPeer;
        private readonly Dictionary<ulong, NodeProxy> _connectedNodes;
        private long _updateLoopRunning = 0;
        private AutoResetEvent _updateLoopFinishedEvent;
        private ProxyFactory _proxyFactory;
        private NodeEntityRepository _entityRepository = new NodeEntityRepository();
        private Dictionary<IPEndPoint, PendingConnection> _pendingConnections = new Dictionary<IPEndPoint, PendingConnection>();
        private Dictionary<uint, PendingOperation> _pendingOperationsByRequestId = new Dictionary<uint, PendingOperation>();
        private uint _requestId = 0;
        private Random _random = new Random();
        private IPersistanceProvider _entityStorage = new MembasePersistance();

        public IDomain Domain
        {
            get { return _domain; }
        }

        public IPersistanceProvider EntityStorage
        {
            get { return _entityStorage; }
        }

        public Node(NodeType type, IDomain domain, IPEndPoint selfEndpoint, IPEndPoint masterNodeEndPoint = null)
        {
            _masterNodeEndPoint = masterNodeEndPoint;
            _selfEndpoint = selfEndpoint;
            if(type == NodeType.Client && _masterNodeEndPoint == null)
                throw new Exception("Client node must have valid masterNodeEndpoint");
            NType = type;
            _domain = domain;

            _netPeer = new RakPeerInterface();
            _reader.SetBuffer(new byte[1024*1024*10], 0);
            _proxyFactory = new ProxyFactory(this);
        }

        public void Start(bool manualUpdate)
        {
            var res = _netPeer.Startup(_selfEndpoint, 1000, 10000);
            if (res != StartupResult.RAKNET_STARTED)
                throw new Exception("NetLayer failed to start:" + res);

            var endpoint = _netPeer.GetActiveEndPoint();
            _selfDescription = new NodeDescription(_netPeer.Id(), NType, endpoint);
            

            if (!manualUpdate)
            {
                _updateLoopFinishedEvent = new AutoResetEvent(false);
                _updateLoopRunning = 1;
                var thread = new Thread(UpdateLoop);
                thread.Start();
            }
        }

        public void Stop()
        {
            if (_updateLoopRunning == 1)
            {
                Interlocked.Exchange(ref _updateLoopRunning, 0);
                _updateLoopFinishedEvent.WaitOne();
            }
            _netPeer.Shutdown();

        }

        private List<PendingConnection> _expiredConnections = new List<PendingConnection>();
        private List<PendingOperation> _expiredOperations = new List<PendingOperation>();
        public void Update()
        {
            var netId = new NetId();
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                int length = 0;
                if (_netPeer.Receive(ref netId, _reader.GetBuffer(), ref length))
                {
                    _reader.SetPosition(0);
                    _reader.SetLength(length);
                    ProcessNetEvent(netId, _reader);    
                }
            }
            
            #region clear expired continuations
            _expiredConnections.Clear();
            foreach (var cont in _pendingConnections.Values)
            {
                if ((DateTime.Now - cont.StartTime).TotalSeconds > 30)
                    _expiredConnections.Add(cont);
            }
            foreach (var expiredConnection in _expiredConnections)
            {
                _pendingConnections.Remove(expiredConnection.Target);
                expiredConnection.TCS.SetException(new Exception("Expired"));
            }

            _expiredOperations.Clear();
            foreach (var cont in _pendingOperationsByRequestId.Values)
            {
                if ((DateTime.Now - cont.StartTime).TotalSeconds > 30)
                    _expiredOperations.Add(cont);
            }
            foreach (var expiredOperation in _expiredOperations)
            {
                _pendingOperationsByRequestId.Remove(expiredOperation.RequestId);
                expiredOperation.TCS.SetException(new Exception("Expired"));
            }
            #endregion

        }

        private void UpdateLoop()
        {
            Log.Info("UpdateLoop started");
            while (Interlocked.Read(ref _updateLoopRunning) == 1)
            {
                Update();
                Thread.Sleep(1);
            }
            Log.Info("UpdateLoop stopped");
            _updateLoopFinishedEvent.Set();

        }

        private void ProcessNetEvent(NetId netId, NativeReader reader)
        {
            var netMsgId = (RakNetMessages)reader.ReadByte();
            switch (netMsgId)
            {
                case RakNetMessages.ID_USER_PACKET_ENUM:
                    Log.Info("Message from NetId<{0}>", netId.Id);
                    uint msgId = reader.ReadUInt32();
                    if(msgId == (uint)NodeMessageId.NodeIntroductionReply)
                        OnNodeIntroductionReply(netId, new NodeIntroductionReply(reader));
                    else if (msgId == (uint)NodeMessageId.NodeIntroductionRequest)
                        OnNodeIntroductionRequest(netId, new NodeIntroductionRequest(reader));
                    else
                    {
                        Message msg = _domain.Deserialize(msgId, reader);
                        if (msg == null)
                            Log.Warn("Cant deserialize message with Id:" + msgId);
                        NodeProxy node;
                        if (_connectedNodes.TryGetValue(netId, out node))
                            OnNodeMessage(node, msg);
                        else
                            Log.Warn("unconnected message");
                    }
                    break;
                case RakNetMessages.ID_DETECT_LOST_CONNECTIONS:
                case RakNetMessages.ID_CONNECTION_LOST:
                case RakNetMessages.ID_DISCONNECTION_NOTIFICATION:
                    OnNetDisconnect(netId);
                    break;
                case RakNetMessages.ID_NEW_INCOMING_CONNECTION:
                    OnNetConnected(netId);
                    Log.Debug("New incoming connection from Node<Id:{1}>", netId.Id);
                    break;
                case RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED:
                    OnNetConnectionAccepted(netId);
                    break;
                default:
                    Log.Warn("Unhandled MessageType:{0} from Node<Id:{1}>", netMsgId, netId.Id);
                    break;
            }
        }

        private void OnNetConnected(NetId senderId)
        {
            Log.Info("NetId<{0}> has connected", senderId.Id);
        }

        private void OnNetConnectionAccepted(NetId senderId)
        {
            Log.Info("Connection to NetId<{0}>  has been accepted", senderId.Id);

            Send(senderId, new NodeIntroductionRequest(_selfDescription));
        }

        private void OnNetDisconnect(NetId senderId)
        {
            Log.Info("NetId<{0}> has disconnected", senderId.Id);
            NodeProxy node;
            if (_connectedNodes.TryGetValue(senderId, out node))
            {
                Log.Info("{0} has disconnected", node);
                _connectedNodes.Remove(senderId);
                OnNodeDisconnected(node);
            }
        }

        private void OnNodeIntroductionRequest(NetId senderId, NodeIntroductionRequest msg)
        {
            Log.Info("OnNodeIntroductionRequest<NetId:{0}, {1}>", senderId.Id, msg.Description);
            var node = new NodeProxy(senderId, msg.Description, this);
            _connectedNodes.Add(senderId, node);
            OnNodeConnected(node);

            PendingConnection continuation;
            if (_pendingConnections.TryGetValue(node.Description.EndPoint, out continuation))
            {
                continuation.TCS.SetResult(node);
                _pendingConnections.Remove(node.Description.EndPoint);
            }
            Send(senderId, new NodeIntroductionReply(_selfDescription));
        }

        private void OnNodeIntroductionReply(NetId senderId, NodeIntroductionReply msg)
        {
            Log.Info("OnNodeIntroductionReply<NetId:{0}, {1}>", senderId.Id, msg.Description);
            NodeProxy node = new NodeProxy(senderId, msg.Description, this);
            _connectedNodes.Add(senderId, node);
            OnNodeConnected(node);

            PendingConnection continuation;
            if (_pendingConnections.TryGetValue(node.Description.EndPoint, out continuation))
            {
                continuation.TCS.SetResult(node);
                _pendingConnections.Remove(node.Description.EndPoint);
            }
            else
                throw new Exception("Received OnNodeIntroductionReply without pending connection");
        }

        public virtual void OnNodeConnected(NodeProxy source)
        {}

        public virtual void OnNodeDisconnected(NodeProxy source)
        {}

        public virtual async void OnNodeMessage(NodeProxy source, Message msg)
        {
            var transportHeader = msg.GetHeader<TransportHeader>();
            #region Process Request
            var requestHeader = msg.GetHeader<EntityOperationRequest>();
            if(requestHeader != null)
            {
                NodeEntity entity;
                if (_entityRepository.TryGet(requestHeader.TargetEntityId, out entity)) //check if we have such entity
                    ProcessEntityOperation(source, requestHeader, transportHeader, entity, msg);
                else if(requestHeader.ActivateIfNotFound)//should we  create one?
                    ActivateAndProcessEntityOperation(source, requestHeader, transportHeader, msg);
                else //route to master or choose creation node if we are master
                {
                    if (NType == NodeType.Master)
                    {
                        NodeProxy target = GetBalancedCreationTarget();
                        if(target != null) //create on some server node
                        {
                            requestHeader.ActivateIfNotFound = true;
                            if (transportHeader != null)
                                target.Send(msg);
                            else
                            {
                                if (source.Description.Type == NodeType.Client)//use routing because only server nodes are connected all to all
                                    msg.AttachHeader(new TransportHeader(Id, source.Description.NodeId));
                                else
                                    msg.AttachHeader(new TransportHeader(source.Description.NodeId, null));
                                target.Send(msg);
                            }
                        }
                        else //create on us
                            ActivateAndProcessEntityOperation(source, requestHeader, transportHeader, msg);
                    }
                }
            }
            #endregion

            #region Process Reply
            var replyHeader = msg.GetHeader<EntityOperationReply>();
            if(replyHeader != null)
            {
                if (transportHeader != null && transportHeader.RoutedNodeId.HasValue)//route reply to source node
                {
                    NodeProxy target;
                    if (_connectedNodes.TryGetValue(transportHeader.RoutedNodeId.Value, out target))
                        target.Send(msg);
                    else
                        Log.Warn("Cant route reply to disconnected Node<Id:{0}>", transportHeader.RoutedNodeId.Value);
                }
                else
                    ProcessEntityOperationReply(replyHeader, msg);
            }
            #endregion
        }

        private void ProcessEntityOperationReply(EntityOperationReply replyHeader, Message msg)
        {
            PendingOperation continuation;
            if(_pendingOperationsByRequestId.TryGetValue(replyHeader.RequestId, out continuation))
            {
                continuation.Caller.CachedNodeId = replyHeader.SourceNodeId;
                continuation.TCS.SetResult(msg);
            }
            else
                Log.Warn("Received Reply with requestId:{0} from nodeId:{0} with no continuation for it",
                    replyHeader.RequestId, replyHeader.SourceNodeId);
        }

        private async void ProcessEntityOperation(NodeProxy source, EntityOperationRequest requestHeader, 
            TransportHeader transportHeader, NodeEntity entity, Message msg)
        {
            Message reply = await _domain.Dispatch(entity, msg);
            if (reply != null)
            {
                reply.AttachHeader(new EntityOperationReply(Id, requestHeader.RequestId));
                SendToIssuer(source, transportHeader, reply);
            }
        }

        private async void ActivateAndProcessEntityOperation(NodeProxy source, EntityOperationRequest requestHeader,
            TransportHeader transportHeader, Message msg)
        {
            NodeEntity entity = await EntityStorage.LoadAsync(requestHeader.TargetEntityId);
            if (entity == null)
                entity = _domain.Create(requestHeader.TargetEntityId);

            Message reply = await _domain.Dispatch(entity, msg);
            if (reply != null)
            {
                reply.AttachHeader(new EntityOperationReply(Id, requestHeader.RequestId));
                SendToIssuer(source, transportHeader, reply);
            }
        }

        private void SendToIssuer(NodeProxy source, TransportHeader transportHeader, Message reply)
        {
            if (transportHeader != null)
            {
                reply.AttachHeader(transportHeader);
                NodeProxy target;
                if (_connectedNodes.TryGetValue(transportHeader.SourceNodeId, out target))
                    target.Send(reply);
                else
                    Log.Warn("Cant reply to disconnected Node<Id:{0}>", transportHeader.SourceNodeId);
            }
            else
                source.Send(reply);
        }

        public Task<NodeProxy> ConnectAsync(IPEndPoint endPoint)
        {
            PendingConnection continuation;

            //check if we are already connected to this endpoint
            foreach (var node in _connectedNodes.Values)
            {
                if (endPoint == node.Description.EndPoint)
                {
                    var tcs = new TaskCompletionSource<NodeProxy>();
                    tcs.SetResult(node);
                    return tcs.Task;
                }
            }

            if (!_pendingConnections.TryGetValue(endPoint, out continuation))
            {
                continuation = new PendingConnection(endPoint);
                _pendingConnections.Add(endPoint, continuation);
                _netPeer.Connect(endPoint);
            }
            return continuation.TCS.Task;
        }

        public void Send(NetId netId, Message msg)
        {
            Log.Trace("Sending to Node<Id:{0}>  msg:{1}", netId.Id, msg);
            _writer.Position = 0;
            _writer.Write((byte)RakNetMessages.ID_USER_PACKET_ENUM);
            msg.Serialize(_writer);
            
            _netPeer.Send(netId, _writer.Buff, (int)_writer.Position, msg.Priority, msg.Reliability, 0, false);
        }

        public void SendLoopback(Message msg)
        {
            Log.Trace("Sending loopback msg:"+msg);
            _writer.Position = 0;
            _writer.Write((byte)RakNetMessages.ID_USER_PACKET_ENUM);
            Domain.Serialize(msg, _writer);

            _netPeer.SendLoopback(_writer.Buff, (int)_writer.Position);
        }

        public TEntityContract GetProxy<TEntityContract>(uint? entityId = null)
            where TEntityContract : class
        {
            ulong fullId = Domain.GetEntityFullId<TEntityContract>(entityId);
            return (TEntityContract)(object)_proxyFactory.GetProxy(fullId);
        }

        public async Task<object> EntityOperationCallAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
            where TRequestMessage : Message
        {
            uint requestId = _requestId++;
            
            var continuation = new PendingOperation(requestId, caller);
            _pendingOperationsByRequestId.Add(requestId, new PendingOperation(requestId, caller));

            input.AttachHeader(new EntityOperationRequest(requestId, caller.EntityId, activateIfNotFound: false));

            if (_entityRepository.Contains(caller.EntityId))
                SendLoopback(input);
            else
            {
                NodeProxy targetNode;
                //first check if we already know where this entity is located
                if (caller.CachedNodeId != null && _connectedNodes.TryGetValue(caller.CachedNodeId.Value, out targetNode))
                {
                    Log.Trace("Sending  to cached: " + targetNode);
                    targetNode.Send(input);
                }
                //if we are master then choose where this entity will be created
                else if (NType == NodeType.Master)
                {
                    targetNode = GetBalancedCreationTarget();
                    if (targetNode == null)//we are the only server node
                        SendLoopback(input);
                    else
                    {
                        input.GetHeader<EntityOperationRequest>().ActivateIfNotFound = true;
                        targetNode.Send(input);
                    }
                }
                else
                {
                    if (!IsMasterConnected)
                        targetNode = await ConnectAsync(_masterNodeEndPoint);
                    else
                        Master.Send(input);
                }
            }
            return continuation.TCS.Task;
        }

        private NodeProxy GetBalancedCreationTarget()
        {
            var nodes = _connectedNodes.Values.Where(x => x.Description.Type == NodeType.Server).ToList();
            if (nodes.Count == 0)//we are solo so create entity on us
                return null;
            else
            {
                //change this in future to real loadbalancing
                var pos = _random.Next(nodes.Count);
                return nodes[pos];
            }
        }
        
        //public abstract async Task EntityCallAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;

        //public abstract async void EntityCallOnewayAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;

        public bool IsMasterConnected
        {
            get
            {
                if (_masterNodeEndPoint == null) //we are master
                    return true;
                return _connectedNodes.Values.Where(x => x.Description.Type == NodeType.Master).Count() > 0;
            }
        }

        public NodeProxy Master
        {
            get { return _connectedNodes.Values.Where(x => x.Description.Type == NodeType.Master).First(); }
        }

        public ulong Id
        {
            get { return _selfDescription.NodeId; }
        }
    }

//    public class ServerNode : Node
//    {
//        public ServerNode(IPEndPoint selfEndpoint, IPEndPoint masterNodeEndPoint)
//            : base(NodeType.Server, selfEndpoint, masterNodeEndPoint)
//        {
//        }
//    }


//    public class ClientNode : Node
//    {
//        public ClientNode(IPEndPoint selfEndpoint, IPEndPoint masterNodeEndPoint)
//            : base(NodeType.Client, selfEndpoint, masterNodeEndPoint)
//        {
//        }

//        public override async Task<TReplyMessage> EntityCallAsync<TRequestMessage, TReplyMessage>(TRequestMessage input, NodeEntityProxy caller)
//        {
//            throw new NotImplementedException();
//        }
//    }
}
