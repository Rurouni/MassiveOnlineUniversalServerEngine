using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NLog;
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
        public readonly TaskCompletionSource<Message> TCS;
        public readonly DateTime StartTime;
        public readonly uint RequestId;
        public readonly NodeEntityProxy Caller;

        public PendingOperation(uint requestId, NodeEntityProxy caller)
        {
            TCS = new TaskCompletionSource<Message>();
            StartTime = DateTime.Now;
            RequestId = requestId;
            Caller = caller;
        }
    }

    public class NodeProxy
    {
        public readonly int NetId;
        public readonly NodeDescription Description;
        public readonly Node Owner;

        public NodeProxy(int netId, NodeDescription description, Node owner)
        {
            NetId = netId;
            Description = description;
            Owner = owner;
        }

        public ulong Id
        {
            get { return Description.NodeId; }
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
    public class Node : INetEventProcessor
    {
        private const int MaxMessagesPerTick = 100000;

        public Logger Log = LogManager.GetCurrentClassLogger();

        public readonly NodeType NType;
        private readonly IEntityDomain _domain;
        private NodeDescription _selfDescription;
        private readonly IPEndPoint _masterNodeEndPoint;
        private readonly IPEndPoint _selfEndpoint;
        private readonly NativeReader _reader = new NativeReader();
        private readonly NativeWriter _writer = new NativeWriter();
        private readonly INetPeer _netPeer;
        private readonly Dictionary<int, NodeProxy> _connectedNodesByNetId = new Dictionary<int,NodeProxy>();
        private readonly Dictionary<ulong, NodeProxy> _connectedNodesByNodeId = new Dictionary<ulong, NodeProxy>();
        private long _updateLoopRunning = 0;
        private AutoResetEvent _updateLoopFinishedEvent;
        private IEntityRepository _entityRepository;
        private IMessageFactory _messageFactory;
        private Dictionary<IPEndPoint, PendingConnection> _pendingConnections = new Dictionary<IPEndPoint, PendingConnection>();
        private Dictionary<uint, PendingOperation> _pendingOperationsByRequestId = new Dictionary<uint, PendingOperation>();
        private Dictionary<ulong, NodeProxy> _entityRoutingTable = new Dictionary<ulong, NodeProxy>();
        private uint _requestId = 0;
        private Random _random = new Random();

        public IEntityDomain Domain
        {
            get { return _domain; }
        }

        
        public IEntityRepository Repository
        {
            get { return _entityRepository; }
        }

        public IMessageFactory MessageFactory
        {
            get { return _messageFactory; }
        }

        public Node(NodeType type, INetPeer netPeer, IEntityDomain domain, IEntityRepository repository, IMessageFactory factory, IPEndPoint selfEndpoint, IPEndPoint masterNodeEndPoint = null)
        {
            _masterNodeEndPoint = masterNodeEndPoint;
            _selfEndpoint = selfEndpoint;
            if(type == NodeType.Client && _masterNodeEndPoint == null)
                throw new Exception("Client node must have valid masterNodeEndpoint");
            NType = type;
            _domain = domain;

            _entityRepository = repository;
            _messageFactory = factory;

            _netPeer = netPeer;
            _reader.SetBuffer(new byte[1024*1024*10], 0);
        }

        public void Start(bool manualUpdate)
        {
            if (_netPeer.Startup(_selfEndpoint, 1000))
                throw new Exception("NetLayer failed to start");

            var endpoint = _netPeer.EndPoint; //this ensures we have valid external endpoint even if _selfEndpoint is set to null to autodetect all
            _selfDescription = new NodeDescription(GenerateUniqueId(), NType, endpoint);
            
            if (!manualUpdate)
            {
                _updateLoopFinishedEvent = new AutoResetEvent(false);
                _updateLoopRunning = 1;
                var thread = new Thread(UpdateLoop);
                thread.Start();
            }
        }

        private ulong GenerateUniqueId()
        {
            var rand = new Random();
            return ((ulong)rand.Next() << 32) ^ (ulong)rand.Next();
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
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                if (!_netPeer.ProcessNetEvent(this))
                    break;
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

        public void OnNetConnect(int netId)
        {
            Log.Info("NetId<{0}> has connected", netId);
        }

        public void OnNetConnectionAccepted(int netId)
        {
            Log.Info("Connection to NetId<{0}>  has been accepted", netId);
            var msg =_messageFactory.New<ConnectRequest>();
            msg.Description = _selfDescription;
            Send(netId, msg);
            _messageFactory.Free(msg);
        }

        public void OnNetDisconnect(int netId)
        {
            Log.Info("NetId<{0}> has disconnected", netId);
            NodeProxy node;
            if (_connectedNodesByNetId.TryGetValue(netId, out node))
            {
                Log.Info("{0} has disconnected", node);
                _connectedNodesByNetId.Remove(netId);
                _connectedNodesByNodeId.Remove(node.Id);
                //TODO: remove all cached entities that were located in this node
                OnNodeDisconnected(node);
                
            }
        }

        public void OnNetData(int netId, NativeReader reader)
        {
            Log.Info("Message from NetId<{0}>", netId);
            Message msg = _messageFactory.Deserialize(reader);
            if (msg.Id == (uint)NodeMessageId.ConnectionReply)
                OnConnectionReply(netId, (ConnectReply)msg);
            else if (msg.Id == (uint)NodeMessageId.ConnectionRequest)
                OnConnectionRequest(netId, (ConnectRequest)msg);
            else if (msg.Id == (uint)NodeMessageId.UpdateClusterInfo)
                OnUpdateClusterInfo((UpdateClusterInfo)msg);
            else
            {
                NodeProxy node;
                if (_connectedNodesByNetId.TryGetValue(netId, out node))
                    OnNodeMessage(node, msg);
                else
                    Log.Warn("unconnected message");
            }
        }

        private void OnConnectionRequest(int senderId, ConnectRequest msg)
        {
            Log.Info("OnConnectionRequest<NetId:{0}, {1}>", senderId, msg.Description);
            var node = new NodeProxy(senderId, msg.Description, this);
            _connectedNodesByNetId.Add(node.NetId, node);
            _connectedNodesByNodeId.Add(node.Id, node);
            OnNodeConnected(node);

            PendingConnection continuation;
            if (_pendingConnections.TryGetValue(node.Description.EndPoint, out continuation))
            {
                continuation.TCS.SetResult(node);
                _pendingConnections.Remove(node.Description.EndPoint);
            }

            var connectReply = _messageFactory.New<ConnectReply>();
            connectReply.Description = _selfDescription;
            Send(senderId, connectReply);
            _messageFactory.Free(connectReply);

            if(msg.Description.Type != NodeType.Client)
            {
                var clusterInfo = _messageFactory.New<UpdateClusterInfo>();
                clusterInfo.Descriptions = _connectedNodesByNetId.Values
                    .Where(x => x.Description.Type != NodeType.Client)
                    .Select(x => x.Description).ToList();
                Send(senderId, clusterInfo);
                _messageFactory.Free(clusterInfo);
            }
        }

        private void OnConnectionReply(int senderId, ConnectReply msg)
        {
            Log.Info("OnConnectionReply<NetId:{0}, {1}>", senderId, msg.Description);
            var node = new NodeProxy(senderId, msg.Description, this);
            _connectedNodesByNetId.Add(node.NetId, node);
            _connectedNodesByNodeId.Add(node.Id, node);
            OnNodeConnected(node);

            PendingConnection continuation;
            if (_pendingConnections.TryGetValue(node.Description.EndPoint, out continuation))
            {
                continuation.TCS.SetResult(node);
                _pendingConnections.Remove(node.Description.EndPoint);
            }
            else
                throw new Exception("Received OnConnectionReply without pending connection");
        }

        private async void OnUpdateClusterInfo(UpdateClusterInfo msg)
        {
            Log.Info("OnConnectionRequest<Nodes in cluster:{0}>", msg.Descriptions.Count);

            foreach (var nodeDescription in msg.Descriptions)
                await Connect(nodeDescription.EndPoint);
        }

        public virtual void OnNodeConnected(NodeProxy source)
        {
        }

        public virtual void OnNodeDisconnected(NodeProxy source)
        {
            foreach (var entity in _entityRepository.Where(entity => entity.Description.Connectionfull))
                entity.OnNodeDisconnected(source);
        }

        public async void OnNodeMessage(NodeProxy source, Message msg)
        {
            var transportHeader = msg.GetHeader<TransportHeader>();

            #region Process Request
            var requestHeader = msg.GetHeader<EntityOperationRequest>();
            
            var entityRoutingHeader = msg.GetHeader<UpdateEntityRoutingHeader>();
            if(entityRoutingHeader != null)
            {
                NodeProxy entityOwner;
                if(_connectedNodesByNodeId.TryGetValue(entityRoutingHeader.OwnerNodeId, out entityOwner))
                    _entityRoutingTable.Add(entityRoutingHeader.EntityId, entityOwner);
                else
                    Log.Warn("Cant update entity routing table for entityId:{0} because nodeId:{1} is not connected",
                             entityRoutingHeader.EntityId, entityRoutingHeader.OwnerNodeId);
            }
            if(requestHeader != null)
            {
                NodeEntity entity;
                NodeProxy entityOwner;
                //check if we have this entity
                if (Repository.TryGet(requestHeader.TargetEntityId, out entity)) 
                    ProcessEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
                //check if we know where it is located
                else if(_entityRoutingTable.TryGetValue(requestHeader.TargetEntityId, out entityOwner))
                    RouteEntityOperationRequest(source, msg, transportHeader, entityOwner);
                //route to master or choose creation node if we are master
                else 
                {
                    if (NType == NodeType.Master) //activate on some server node
                    {
                        entityOwner = GetBalancedCreationTarget();
                        if(entityOwner != null) //activate on some server node
                        {
                            _entityRoutingTable.Add(requestHeader.TargetEntityId, entityOwner);
                            RouteEntityOperationRequest(source, msg, transportHeader, entityOwner);
                        }
                        else //activate on master
                        {
                            entity = await ActivateEntity(requestHeader.TargetEntityId);
                            ProcessEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
                        }
                    }
                    else if( source.Description.Type == NodeType.Master) // master send us this message so activate
                    {
                        entity = await ActivateEntity(requestHeader.TargetEntityId);
                        ProcessEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
                    }
                    else
                        RouteEntityOperationRequest(source, msg, transportHeader, Master);
                }
            }
            #endregion

            #region Process Reply
            var replyHeader = msg.GetHeader<EntityOperationReply>();
            if(replyHeader != null)
            {
                //if we have transport header at this point then it can be only because of routed request
                if (transportHeader != null)
                {
                    NodeProxy target;
                    if (_connectedNodesByNodeId.TryGetValue(transportHeader.RoutedNodeId.Value, out target))
                    {
                        msg.RemoveHeader<TransportHeader>(); //its not needed anymore
                        target.Send(msg);
                    }
                    else
                        Log.Warn("Cant route reply to disconnected Node<Id:{0}>", transportHeader.RoutedNodeId.Value);
                }
                else
                {
                    ProcessEntityOperationReply(replyHeader, msg);
                }
            }
            #endregion
        }

        private void RouteEntityOperationRequest(NodeProxy source, Message msg, TransportHeader transportHeader,
            NodeProxy target)
        {
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

        private void ProcessEntityOperationReply(EntityOperationReply replyHeader, Message msg)
        {
            PendingOperation continuation;
            if(_pendingOperationsByRequestId.TryGetValue(replyHeader.RequestId, out continuation))
            {
                _pendingOperationsByRequestId.Remove(replyHeader.RequestId);
                if (msg.Id == (uint)NodeMessageId.InvalidEntityOperation)
                    continuation.TCS.SetException(new InvalidEntityOperationException());
                else
                    continuation.TCS.SetResult(msg);
            }
            else
                Log.Warn("Received Reply with requestId:{0}  with no continuation for it",
                    replyHeader.RequestId);
        }

        private async void ProcessEntityOperationRequest(NodeProxy source, EntityOperationRequest requestHeader, 
            TransportHeader transportHeader, NodeEntity entity, Message msg)
        {
            Message reply;
            if (msg.Id == (uint)NodeMessageId.EntityDiscoveryRequest)
            {
                var discoveryReply = _messageFactory.New<EntityDiscoveryReply>();
                discoveryReply.Description = _selfDescription;
                reply = discoveryReply;
            }
            else
                reply = await _domain.Dispatch(entity, msg);

            reply.AttachHeader(new EntityOperationReply(requestHeader.RequestId));
            if (transportHeader != null)
            {
                NodeProxy target;
                if (_connectedNodesByNodeId.TryGetValue(transportHeader.SourceNodeId, out target))
                {
                    if (transportHeader.RoutedNodeId.HasValue)
                        reply.AttachHeader(transportHeader);
                    reply.AttachHeader(new UpdateEntityRoutingHeader(entity.Id, Id));

                    target.Send(reply);
                }
                else
                    Log.Warn("Cant reply to disconnected Node<Id:{0}>", transportHeader.SourceNodeId);
            }
            else
                source.Send(reply);

            _messageFactory.Free(reply);
        }

        public async Task<NodeEntity> ActivateEntity(ulong entityId)
        {
            NodeEntity entity;
            if (Repository.TryGet(entityId, out entity))
                Log.Warn("{0} is already activated", entity);
            else
                entity = await Repository.Activate(entityId);

            return entity;
        }

        public async Task DeleteEntity(ulong entityId)
        {
            NodeEntity entity;
            if (Repository.TryGet(entityId, out entity))
                await Repository.Delete(entity);
            else
                Log.Warn("Cant delete Entity<Id:{0}> - not found", entityId);
        }

        public Task<NodeProxy> Connect(IPEndPoint endPoint)
        {
            PendingConnection continuation;

            //check if we are already connected to this endpoint
            foreach (var node in _connectedNodesByNodeId.Values)
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

        public async Task<NodeProxy> ConnectToEntityOwner(NodeEntityProxy proxy)
        {
            Message reply = await ExecuteEntityOperationAsync(new EntityDiscoveryRequest(), proxy);
            NodeProxy target = await Connect(((EntityDiscoveryReply) reply).Description.EndPoint);
            return target;
        }

        public void Send(int netId, Message msg)
        {
            Log.Trace("Sending to Node<Id:{0}>  msg:{1}", netId, msg);
            _writer.Position = 0;
            msg.Serialize(_writer);
            
            _netPeer.Send(netId, _writer.Buff, (int)_writer.Position, msg.Priority, msg.Reliability);
        }

        public void SendLoopback(Message msg)
        {
            Log.Trace("Sending loopback msg:"+msg);
            _writer.Position = 0;
            msg.Serialize(_writer);
            
            _netPeer.SendLoopback(_writer.Buff, (int)_writer.Position);
        }

        public TEntityContract GetProxy<TEntityContract>(uint? entityId = null)
            where TEntityContract : class
        {
            ulong fullId = Domain.GetFullId<TEntityContract>(entityId);
            return (TEntityContract)(object)Domain.GetProxy(fullId);
        }

        public async Task<Message> ExecuteEntityOperationAsync(Message input, NodeEntityProxy proxy)
        {
            uint requestId = _requestId++;
            
            input.AttachHeader(new EntityOperationRequest(requestId, proxy.EntityId));

            if (Repository.Contains(proxy.EntityId))
                SendLoopback(input);
            else
            {
                NodeProxy targetNode;
                //first check if we already know where thsi entity is located
                if (_entityRoutingTable.TryGetValue(proxy.EntityId, out targetNode))
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
                        _entityRoutingTable.Add(proxy.EntityId, targetNode);
                        targetNode.Send(input);
                    }
                }
                else
                {
                    if (proxy.EntityDescription.Connectionfull)
                    {
                        targetNode = await ConnectToEntityOwner(proxy);
                        targetNode.Send(input);
                    }
                    else
                        Master.Send(input);
                }
            }
            Message reply = await AwaitEntityOperationReply(requestId, proxy);

            return reply;
        }

        public Task<Message> AwaitEntityOperationReply(uint requestId, NodeEntityProxy requestOwner)
        {
            var continuation = new PendingOperation(requestId, requestOwner);
            _pendingOperationsByRequestId.Add(requestId, continuation);
            return continuation.TCS.Task;
        }

        private NodeProxy GetBalancedCreationTarget()
        {
            var nodes = _connectedNodesByNodeId.Values.Where(x => x.Description.Type == NodeType.Server).ToList();
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
                return _connectedNodesByNodeId.Values.Where(x => x.Description.Type == NodeType.Master).Count() > 0;
            }
        }

        public NodeProxy Master
        {
            get { return _connectedNodesByNodeId.Values.Where(x => x.Description.Type == NodeType.Master).First(); }
        }

        public ulong Id
        {
            get { return _selfDescription.NodeId; }
        }
    }

    public class InvalidEntityOperationException : Exception
    {
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
