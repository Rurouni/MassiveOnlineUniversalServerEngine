using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
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

    public class OperationContext
    {
        public Message Message;
        public INode Node;
        public NodeProxy Source;

        public void SetData(Message msg, NodeProxy source)
        {
            Message = msg;
            Source = source;
        }

        public OperationContext(INode node, Message message, NodeProxy source)
        {
            Node = node;
            Message = message;
            Source = source;
        }
    }

    public interface INode
    {
        IMessageFactory MessageFactory { get; }

        void Start(bool manualUpdate, IPEndPoint selfEndpoint = null);
        void Stop();
        void Update();

        Task<NodeProxy> Connect(IPEndPoint endPoint);
        void Send(int netId, Message msg);
        void Send(ulong nodeId, Message msg);

        IObservable<NodeProxy> OnNodeConnected { get; }
        IObservable<NodeProxy> OnNodeDisconnected { get; }
        IObservable<OperationContext> OnNodeMessage { get; }
    }

    public interface IEntityClusterNode : INode
    {
        IEntityDomain Domain { get; }
        IEntityRepository Repository { get; }

        TEntityContract GetProxy<TEntityContract>(uint? entityLocalId = null, NodeProxy target = null) where TEntityContract : class;
        Task<Message> Execute(Message input, NodeEntityProxy proxy);
    }

    public class NodeProxy
    {
        public readonly int NetId;
        public readonly NodeDescription Description;
        public readonly INode Owner;

        public NodeProxy(int netId, NodeDescription description, INode owner)
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
    public class Node : INode, INetEventProcessor
    {
        private const int MaxMessagesPerTick = 100000;

        public Logger Log = LogManager.GetCurrentClassLogger();

        private NodeDescription _selfDescription;

        protected readonly NativeReader _reader = new NativeReader();
        protected readonly NativeWriter _writer = new NativeWriter();
        protected readonly Dictionary<int, NodeProxy> _connectedNodesByNetId = new Dictionary<int,NodeProxy>();
        protected readonly Dictionary<ulong, NodeProxy> _connectedNodesByNodeId = new Dictionary<ulong, NodeProxy>();
        private long _updateLoopRunning = 0;
        private AutoResetEvent _updateLoopFinishedEvent;
        private Dictionary<IPEndPoint, PendingConnection> _pendingConnections = new Dictionary<IPEndPoint, PendingConnection>();
        private List<PendingConnection> _expiredConnections = new List<PendingConnection>();
        
        protected Random _random = new Random();
        protected OperationContext _operationContext;

        public IMessageFactory MessageFactory { get; set; }
        public INetPeer Net { get; set; }

        public ulong Id
        {
            get { return _selfDescription.NodeId; }
        }

        public OperationContext OperationContext
        {
            get { return _operationContext; }
        }

        public Random Random
        {
            get { return _random; }
        }

        public NodeDescription Description
        {
            get { return _selfDescription; }
        }

        public Node(INetPeer net, IMessageFactory factory)
        {
            Net = net;
            MessageFactory = factory;

            _operationContext = new OperationContext(this, null, null);
            _reader.SetBuffer(new byte[1024*1024*10], 0);
        }

        private ulong GenerateUniqueId()
        {
            var rand = new Random();
            return ((ulong)rand.Next() << 32) ^ (ulong)rand.Next();
        }

        public void Start(bool manualUpdate, IPEndPoint selfEndpoint = null)
        {
            if (!Net.Startup(selfEndpoint, 1000))
                throw new Exception("NetLayer failed to start");

            var endpoint = Net.EndPoint; //this ensures we have valid external endpoint even if _selfEndpoint is set to null to autodetect all
            _selfDescription = new NodeDescription(GenerateUniqueId(), endpoint);
            Log = LogManager.GetLogger(ToString());

            if (!manualUpdate)
            {
                _updateLoopFinishedEvent = new AutoResetEvent(false);
                _updateLoopRunning = 1;
                var thread = new Thread(UpdateLoop);
                thread.Start();
                Log.Info("UpdateThread started");
            }
            Log.Info("Started on {0}", endpoint.ToString());
        }

        public void Stop()
        {
            if (_updateLoopRunning == 1)
            {
                Interlocked.Exchange(ref _updateLoopRunning, 0);
                _updateLoopFinishedEvent.WaitOne();
            }
            Net.Shutdown();

        }

        public void Update()
        {
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                if (!Net.ProcessNetEvent(this))
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

            #endregion

            OnNodeUpdate();

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
            Log.Info("NetId:{0} has connected", netId);
        }

        public void OnNetConnectionAccepted(int netId)
        {
            Log.Info("Connection to NetId:{0}  has been accepted", netId);
            var msg = MessageFactory.New<ConnectRequest>();
            msg.Description = _selfDescription;
            Send(netId, msg);
            MessageFactory.Free(msg);
        }

        public void OnNetDisconnect(int netId)
        {
            Log.Info("NetId:{0} has disconnected", netId);
            NodeProxy node;
            if (_connectedNodesByNetId.TryGetValue(netId, out node))
            {
                Log.Info("{0} has disconnected", node);
                _connectedNodesByNetId.Remove(netId);
                _connectedNodesByNodeId.Remove(node.Id);
                OnNodeDisconnected(node);
            }
        }

        public void OnNetData(int netId, NativeReader reader)
        {
            Log.Info("Message from NetId:{0}", netId);
            Message msg = MessageFactory.Deserialize(reader);
            if (msg.Id == (uint)NodeMessageId.ConnectionReply)
                OnConnectionReply(netId, (ConnectReply)msg);
            else if (msg.Id == (uint)NodeMessageId.ConnectionRequest)
                OnConnectionRequest(netId, (ConnectRequest)msg);
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

            var connectReply = MessageFactory.New<ConnectReply>();
            connectReply.Description = _selfDescription;
            Send(senderId, connectReply);
            MessageFactory.Free(connectReply);
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

        protected virtual void OnNodeUpdate()
        {
            
        }

        protected virtual void OnNodeConnected(NodeProxy source)
        {
            _onNodeConnectedSubject.OnNext(source);
        }

        protected virtual void OnNodeDisconnected(NodeProxy source)
        {
            _onNodeDisconnectedSubject.OnNext(source);
        }

        protected virtual void OnNodeMessage(NodeProxy source, Message msg)
        {
            _operationContext.SetData(msg, source);
            _onNodeMessageSubject.OnNext(_operationContext);
            
        }

        public Task<NodeProxy> Connect(IPEndPoint endPoint)
        {
            Log.Info("Connecting to " + endPoint);
            PendingConnection continuation;
            //check if we are already connected to this endpoint
            foreach (var node in _connectedNodesByNodeId.Values)
            {
                if (endPoint.Equals(node.Description.EndPoint))
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
                Net.Connect(endPoint);
            }
            return continuation.TCS.Task;
        }

        public void Send(int netId, Message msg)
        {
            Log.Trace("sending  msg:{0} over NetId:{1} ", msg, netId);
            _writer.Position = 0;
            msg.Serialize(_writer);

            Net.Send(netId, _writer.Buff, (int)_writer.Position, msg.Priority, msg.Reliability);
        }

        public void Send(ulong nodeId, Message msg)
        {
            NodeProxy proxy;
            if (_connectedNodesByNodeId.TryGetValue(nodeId, out proxy))
            {
                Log.Trace("sending  msg:{0} to {1} ", msg, proxy);
                Send(proxy.NetId, msg);
            }
            else
                Log.Warn("Send->Unknown NodeId:" + nodeId);
        }

        protected void SendLoopback(Message msg)
        {
            Log.Trace("sending loopback msg:{0}", msg);
            _writer.Position = 0;
            msg.Serialize(_writer);

            Net.SendLoopback(_writer.Buff, (int)_writer.Position);
        }

        Subject<NodeProxy> _onNodeConnectedSubject = new Subject<NodeProxy>();
        IObservable<NodeProxy> INode.OnNodeConnected
        {
            get { return _onNodeConnectedSubject; }
        }

        Subject<NodeProxy> _onNodeDisconnectedSubject = new Subject<NodeProxy>();
        IObservable<NodeProxy> INode.OnNodeDisconnected
        {
            get { return _onNodeDisconnectedSubject; }
        }

        Subject<OperationContext> _onNodeMessageSubject = new Subject<OperationContext>();
        IObservable<OperationContext> INode.OnNodeMessage
        {
            get { return _onNodeMessageSubject; }
        }

        public override string ToString()
        {
            return _selfDescription.ToString();
        }
        
    }

    public class InvalidEntityOperationException : Exception
    {
    }

    public class EntityClusterNode : Node, IEntityClusterNode
    {
        private uint _requestId = 0;
        private NodeProxy _masterNode;
        private Dictionary<uint, PendingOperation> _pendingOperationsByRequestId = new Dictionary<uint, PendingOperation>();
        private List<PendingOperation> _expiredOperations = new List<PendingOperation>();
        private Dictionary<ulong, NodeProxy> _entityRoutingTable = new Dictionary<ulong, NodeProxy>();
        private Dictionary<ulong, NodeEntityProxy> _proxyCache = new Dictionary<ulong, NodeEntityProxy>();

        public IEntityDomain Domain { get; set; }
        public IEntityRepository Repository { get; set; }

        public EntityClusterNode(INetPeer net, IMessageFactory factory, IEntityDomain domain, IEntityRepository repository)
            : base(net, factory)
        {
            Domain = domain;
            Repository = repository;
        }

        protected NodeProxy Master
        {
            get { return _masterNode; }
        }

        protected override void OnNodeUpdate()
        {
            base.OnNodeUpdate();

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
        }

        protected override void OnNodeMessage(NodeProxy source, Message msg)
        {
            base.OnNodeMessage(source, msg);

            if (msg.Id == (uint)NodeMessageId.UpdateClusterInfo)
                OnUpdateClusterInfo((UpdateClusterInfo)msg);
            else
            {
                var transportHeader = msg.GetHeader<TransportHeader>();
                ProcessEntityOperationRequest(source, msg, transportHeader);
                ProcessEntityOperationReply(source, msg, transportHeader);
            }
        }

        protected async void ProcessEntityOperationRequest(NodeProxy source, Message msg, TransportHeader transportHeader)
        {
            var requestHeader = msg.GetHeader<EntityOperationRequest>();

            var entityRoutingHeader = msg.GetHeader<UpdateEntityRoutingHeader>();
            if (entityRoutingHeader != null)
            {
                NodeProxy entityOwner;
                if (_connectedNodesByNodeId.TryGetValue(entityRoutingHeader.OwnerNodeId, out entityOwner))
                    _entityRoutingTable.Add(entityRoutingHeader.EntityId, entityOwner);
                else
                    Log.Warn("Cant update entity routing table for entityId:{0} because nodeId:{1} is not connected",
                             entityRoutingHeader.EntityId, entityRoutingHeader.OwnerNodeId);
            }
            if (requestHeader != null)
            {
                NodeEntity entity;
                NodeProxy entityOwner;
                //check if we have this entity
                if (Repository.TryGet(requestHeader.TargetEntityId, out entity))
                    DispatchEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
                //check if we know where it is located
                else if (_entityRoutingTable.TryGetValue(requestHeader.TargetEntityId, out entityOwner))
                    RouteEntityOperationRequest(source, msg, transportHeader, entityOwner);
                //route to master or choose creation node if we are master
                else
                {
                    if (Master == null) // we are master, so choose some server node to activate on
                    {
                        entityOwner = GetBalancedCreationTarget();
                        if (entityOwner != null) //activate on some server node
                        {
                            _entityRoutingTable.Add(requestHeader.TargetEntityId, entityOwner);
                            RouteEntityOperationRequest(source, msg, transportHeader, entityOwner);
                        }
                        else //activate on master
                        {
                            entity = await ActivateEntity(requestHeader.TargetEntityId);
                            DispatchEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
                        }
                    }
                    else if (source == Master) // master send us this message so activate
                    {
                        entity = await ActivateEntity(requestHeader.TargetEntityId);
                        DispatchEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
                    }
                    else
                        RouteEntityOperationRequest(source, msg, transportHeader, Master);
                }
            }
        }

        protected async void ProcessEntityOperationReply(NodeProxy source, Message msg, TransportHeader transportHeader)
        {
            var replyHeader = msg.GetHeader<EntityOperationReply>();
            if (replyHeader != null)
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
                    DispatchEntityOperationReply(replyHeader, msg);
                }
            }
        }

        protected override void OnNodeConnected(NodeProxy source)
        {
            base.OnNodeConnected(source);

            var clusterInfo = MessageFactory.New<UpdateClusterInfo>();
            clusterInfo.Descriptions = _connectedNodesByNetId.Values.Select(x => x.Description).ToList();
            source.Send(clusterInfo);
            MessageFactory.Free(clusterInfo);
        }

        private async void OnUpdateClusterInfo(UpdateClusterInfo msg)
        {
            Log.Info("OnConnectionRequest<Nodes in cluster:{0}>", msg.Descriptions.Count);

            foreach (var nodeDescription in msg.Descriptions)
                await Connect(nodeDescription.EndPoint);
        }


        protected void RouteEntityOperationRequest(NodeProxy source, Message msg, TransportHeader transportHeader,
            NodeProxy target)
        {
            if (transportHeader != null)
                target.Send(msg);
            else
            {
                //support client scenario later
                //if (source.Description.Type == NodeType.Client)//use routing because only server nodes are connected all to all
                //    msg.AttachHeader(new TransportHeader(Id, source.Description.NodeId));
                //else
                    msg.AttachHeader(new TransportHeader(source.Description.NodeId, null));
                target.Send(msg);
            }
        }

        protected void DispatchEntityOperationReply(EntityOperationReply replyHeader, Message msg)
        {
            PendingOperation continuation;
            if (_pendingOperationsByRequestId.TryGetValue(replyHeader.RequestId, out continuation))
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

        protected async void DispatchEntityOperationRequest(NodeProxy source, EntityOperationRequest requestHeader,
            TransportHeader transportHeader, NodeEntity entity, Message msg)
        {
            Message reply;
            if (msg.Id == (uint)NodeMessageId.EntityDiscoveryRequest)
            {
                var discoveryReply = MessageFactory.New<EntityDiscoveryReply>();
                discoveryReply.Description = Description;
                reply = discoveryReply;
            }
            else
                reply = await Domain.Dispatch(entity, msg);

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

            MessageFactory.Free(reply);
        }

        protected async Task<NodeEntity> ActivateEntity(ulong entityId)
        {
            NodeEntity entity;
            if (Repository.TryGet(entityId, out entity))
                Log.Warn("{0} is already activated", entity);
            else
                entity = await Repository.Activate(entityId);

            return entity;
        }

        protected async Task DeleteEntity(ulong entityId)
        {
            NodeEntity entity;
            if (Repository.TryGet(entityId, out entity))
                await Repository.Delete(entity);
            else
                Log.Warn("Cant delete Entity<Id:{0}> - not found", entityId);
        }

        public TEntityContract GetProxy<TEntityContract>(uint? entityId = null, NodeProxy target = null)
            where TEntityContract : class
        {
            ulong fullId = Domain.GetFullId<TEntityContract>(entityId);
            NodeEntityProxy proxy;
            if (!_proxyCache.TryGetValue(fullId, out proxy))
            {
                proxy = Domain.CreateProxy(fullId);
                proxy.Node = this;
                proxy.Target = target;
                _proxyCache.Add(fullId, proxy);
            }
            return (TEntityContract)(object)proxy;
        }

        public async Task<Message> Execute(Message input, NodeEntityProxy proxy)
        {
            uint requestId = _requestId++;

            input.AttachHeader(new EntityOperationRequest(requestId, proxy.EntityId));

            if (Repository.Contains(proxy.EntityId))
                SendLoopback(input);
            else
            {
                NodeProxy targetNode;
                //first check if we already know where this entity is located
                if (_entityRoutingTable.TryGetValue(proxy.EntityId, out targetNode))
                {
                    Log.Trace("Sending  to cached: " + targetNode);
                    targetNode.Send(input);
                }
                //if we are master then choose where this entity will be created
                else if (Master == null)
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

        protected Task<Message> AwaitEntityOperationReply(uint requestId, NodeEntityProxy requestOwner)
        {
            var continuation = new PendingOperation(requestId, requestOwner);
            _pendingOperationsByRequestId.Add(requestId, continuation);
            return continuation.TCS.Task;
        }

        protected NodeProxy GetBalancedCreationTarget()
        {
            var nodes = _connectedNodesByNodeId.Values.ToList();
            if (nodes.Count == 0)//we are solo so create entity on us
                return null;
            else
            {
                //change this in future to real loadbalancing
                var pos = _random.Next(nodes.Count);
                return nodes[pos];
            }
        }

        protected async Task<NodeProxy> ConnectToEntityOwner(NodeEntityProxy proxy)
        {
            Message reply = await Execute(new EntityDiscoveryRequest(), proxy);
            NodeProxy target = await Connect(((EntityDiscoveryReply)reply).Description.EndPoint);
            return target;
        }

        //public abstract async Task EntityCallAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;

        //public abstract async void EntityCallOnewayAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;
        
    }


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
