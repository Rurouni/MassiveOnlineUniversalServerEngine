using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using NLog;

namespace MOUSE.Core
{
    public class ServerFiber
    {
        protected class Issue
        {
            public Func<Task> Func;
            public LockType Lock;

            public Issue(Func<Task> func, LockType @lock)
            {
                Func = func;
                Lock = @lock;
            }
        }

        private readonly ActionBlock<Issue> _issueQueue;
        private readonly ConcurrentExclusiveSchedulerPair _schedulerPair = new ConcurrentExclusiveSchedulerPair();
        private ActionBlock<Func<Task>> _writeLockedQueue;
        private ActionBlock<Func<Task>> _readLockedQueue;

        public ServerFiber()
        {
            _issueQueue = new ActionBlock<Issue>((Func<Issue, Task>)ProcessIssue);
        }

        private void CreateQueues()
        {
            _readLockedQueue = new ActionBlock<Func<Task>>(
                async(func) =>
                {
                    await func();
                }, new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ConcurrentScheduler });
            _writeLockedQueue = new ActionBlock<Func<Task>>(
                async(func) =>
                {
                    await func();
                }, new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ExclusiveScheduler });
        }

        private async Task ProcessIssue(Issue issue)
        {
            switch (issue.Lock)
            {
                case LockType.None:
                    issue.Func();
                    break;
                case LockType.ReadReentrant:
                    _readLockedQueue.Post(issue.Func);
                    break;
                case LockType.WriteReentrant:
                    _writeLockedQueue.Post(issue.Func);
                    break;
                case LockType.Full:
                    _readLockedQueue.Complete();
                    _writeLockedQueue.Complete();
                    await _readLockedQueue.Completion;
                    await _writeLockedQueue.Completion;
                    await issue.Func();
                    CreateQueues();
                    break;
            }
        }

        public void Process(Func<Task> func, LockType lockType)
        {
            _issueQueue.Post(new Issue(func, lockType));
        }
    }

    /// <summary>
    /// assumes sequential events flow from NetPeer
    /// </summary>
    public class ClientPeer : NetPeer
    {
        private readonly ConcurrentDictionary<uint, object> _handlersByNetContractId = new ConcurrentDictionary<uint, object>();
        protected ServerNode Node;
        
        public readonly ServerFiber Fiber;
        
        public ClientPeer(INetChannel channel, ServerNode node) : base(channel, node.ExternalNet)
        {
            Node = node;
            MessageEvent.Subscribe(OnClientMessage);
            Fiber = new ServerFiber();
        }

        protected void SetHandler<TNetContract>(TNetContract implementer)
        {
            _handlersByNetContractId[Node.Protocol.GetContractId(typeof(TNetContract))] = implementer;
        }

        protected void RemoveHandler<TNetContract>()
        {
            object removed;
            _handlersByNetContractId.TryRemove(Node.Protocol.GetContractId(typeof(TNetContract)), out removed);
        }

        private void OnClientMessage(Message msg)
        {
            try
            {
                if (msg is ServiceAccessRequest)
                {
                    var requestMsg = (msg as ServiceAccessRequest);
                    if (_handlersByNetContractId.ContainsKey(Node.Protocol.GetContractId(requestMsg.ServiceId)))
                        Channel.Send(new ServiceAccessReply(true, null));
                    else
                    {
                        var serviceDesc = Node.Protocol.GetDescription(requestMsg.ServiceId);
                        if (serviceDesc == null || !serviceDesc.AllowExternalConnections)
                            Channel.Send(new ServiceAccessReply(false, null));
                        else
                            Node.ProcessServiceAccess(this, requestMsg.ServiceId);
                    }
                }
                else 
                {
                    var serviceHeader = msg.GetHeader<ServiceHeader>();
                    var operationHeader = msg.GetHeader<OperationHeader>();
                    if (operationHeader != null && operationHeader.Type == OperationType.Request && serviceHeader != null)
                    {
                        object handler;
                        if (_handlersByNetContractId.TryGetValue(Node.Protocol.GetContractId(serviceHeader.TargetServiceKey), out handler))
                        {
                            Log.Debug("{0} - Dispatching {1} to {2}", operationHeader.RequestId, msg, handler);
                            if (msg.LockType == LockType.None)
                                DispatchAndReplyAsync(handler, operationHeader.RequestId, msg);
                            else
                                Fiber.Process(() => DispatchAndReplyAsync(handler, operationHeader.RequestId, msg), msg.LockType);
                        }
                        else
                            Node.DispatchClientOperationToService(serviceHeader.TargetServiceKey, msg, this);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString);
                Channel.Close();
            }
        }

        private async Task DispatchAndReplyAsync(object handler, int requestId, Message msg)
        {
            try
            {
                Message reply = await Node.Protocol.Dispatch(handler, msg);
                if (reply != null)
                {
                    reply.AttachHeader(new OperationHeader(requestId, OperationType.Reply));
                    Channel.Send(reply);
                    Log.Debug("{0} - Sending back {1}", requestId, reply);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }


    public class ServerNode
    {
        private readonly ulong _id; 
        public readonly Logger Log;
        private readonly ConcurrentDictionary<NodeServiceKey, NodeServiceProxy> _proxyCache = new ConcurrentDictionary<NodeServiceKey, NodeServiceProxy>();
        //protected readonly Dictionary<ulong, NetPeer> _connectedNodesByNodeId = new Dictionary<ulong, NetPeer>();

        public readonly INetNode<ClientPeer> ExternalNet;
        //protected INetNode<ClientNodePeer> InternalNet;

        private readonly Random _random = new Random();
        protected Random Rnd
        {
            get { return _random; }
        }

        public ulong Id
        {
            get { return _id; }
        }

        public IServiceRepository Repository { get; private set; }
        
        public IServiceProtocol Protocol { get; private set; }

        public IMessageFactory MessageFactory
        {
            get { return ExternalNet.MessageFactory; }
        }

        public ServerNode(Func<INetChannel, ClientPeer> clientPeerFactory, INetProvider externalNetProvider, INetProvider internalNetProvider,
            IMessageFactory factory, IServiceProtocol protocol, IServiceRepository repository)
        {
            _id = GenerateUniqueId();
            Log = LogManager.GetLogger(ToString());
            Repository = repository;
            Protocol = protocol;
            ExternalNet = new NetNode<ClientPeer>(externalNetProvider, factory, protocol, clientPeerFactory);
            //InternalNet = new NetNode<ClientNodePeer>(internalNetProvider, factory, protocol);
        }

        public async Task<TNetContract> GetService<TNetContract>(uint serviceId = 0)
        {
            NodeServiceKey serviceKey = Protocol.GetKey<TNetContract>(serviceId);
            return (TNetContract)(object)_proxyCache.GetOrAdd(serviceKey, createProxy);
        }

        private NodeServiceProxy createProxy(NodeServiceKey serviceKey)
        {
            NodeService service;
            Repository.TryGet(serviceKey, out service);
            var proxy = Protocol.CreateProxy(serviceKey, null, service);
            return proxy;
        }

        public void DispatchClientOperationToService(ulong serviceId, Message msg, ClientPeer clientPeer)
        {
            NodeServiceContractDescription desc = Protocol.GetDescription(serviceId);
            if (desc == null || !desc.AllowExternalConnections)
            {
                Log.Debug("{0} sends operation to invalid or not visible serviceId:{1}", clientPeer, serviceId);
                return;
            }
            NodeService service;
            if (Repository.TryGet(serviceId, out service))
                service.Process(new OperationContext(this, msg, clientPeer));
            else
                Log.Debug("{0} sends operation to non active serviceId:{1}", clientPeer, serviceId);
        }

        public void ProcessServiceAccess(ClientPeer peer, ulong serviceId)
        {
            NodeServiceContractDescription desc = Protocol.GetDescription(serviceId);
            if(desc == null || !desc.AllowExternalConnections)
                peer.Channel.Send(new ServiceAccessReply(false, null));
            else
            {
                NodeDescription ownerNode = GetNodeForService(serviceId);
                if(ownerNode != null)
                    throw new NotImplementedException();

                if (Repository.Contains(serviceId))
                    peer.Channel.Send(new ServiceAccessReply(true, null));
                else
                    peer.Channel.Send(new ServiceAccessReply(false, null));
            }
        }

        private NodeDescription GetNodeForService(ulong serviceId)
        {
            return null;
        }

        private ulong GenerateUniqueId()
        {
            return ((ulong)Rnd.Next() << 32) ^ (ulong)Rnd.Next();
        }

        public override string ToString()
        {
            return string.Format("ServerNode<Id:{0}>", _id);
        }

        //protected override void OnNodeMessage(NetPeer source, Message msg)
        //{
        //    base.OnNodeMessage(source, msg);

        //    if (msg.Id == (uint)NodeMessageId.UpdateClusterInfo)
        //        OnUpdateClusterInfo((UpdateClusterInfo)msg);
        //    else
        //    {
        //        var transportHeader = msg.GetHeader<TransportHeader>();
        //        ProcessEntityOperationRequest(source, msg, transportHeader);
        //        ProcessEntityOperationReply(source, msg, transportHeader);
        //    }
        //}

        //protected async void ProcessEntityOperationRequest(NetPeer source, Message msg, TransportHeader transportHeader)
        //{
        //    var requestHeader = msg.GetHeader<EntityOperationRequest>();

        //    var entityRoutingHeader = msg.GetHeader<UpdateEntityRoutingHeader>();
        //    if (entityRoutingHeader != null)
        //    {
        //        NetPeer entityOwner;
        //        if (_connectedNodesByNodeId.TryGetValue(entityRoutingHeader.OwnerNodeId, out entityOwner))
        //            _entityRoutingTable.Add(entityRoutingHeader.EntityId, entityOwner);
        //        else
        //            Log.Warn("Cant update entity routing table for entityId:{0} because nodeId:{1} is not connected",
        //                     entityRoutingHeader.EntityId, entityRoutingHeader.OwnerNodeId);
        //    }
        //    if (requestHeader != null)
        //    {
        //        NodeService entity;
        //        NetPeer entityOwner;
        //        //check if we have this entity
        //        if (Repository.TryGet(requestHeader.TargetEntityId, out entity))
        //            DispatchEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
        //        //check if we know where it is located
        //        else if (_entityRoutingTable.TryGetValue(requestHeader.TargetEntityId, out entityOwner))
        //            RouteEntityOperationRequest(source, msg, transportHeader, entityOwner);
        //        //route to master or choose creation node if we are master
        //        else
        //        {
        //            if (Master == null) // we are master, so choose some server node to activate on
        //            {
        //                entityOwner = GetBalancedCreationTarget();
        //                if (entityOwner != null) //activate on some server node
        //                {
        //                    _entityRoutingTable.Add(requestHeader.TargetEntityId, entityOwner);
        //                    RouteEntityOperationRequest(source, msg, transportHeader, entityOwner);
        //                }
        //                else //activate on master
        //                {
        //                    entity = await ActivateEntity(requestHeader.TargetEntityId);
        //                    DispatchEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
        //                }
        //            }
        //            else if (source == Master) // master send us this message so activate
        //            {
        //                entity = await ActivateEntity(requestHeader.TargetEntityId);
        //                DispatchEntityOperationRequest(source, requestHeader, transportHeader, entity, msg);
        //            }
        //            else
        //                RouteEntityOperationRequest(source, msg, transportHeader, Master);
        //        }
        //    }
        //}

        //protected async void ProcessEntityOperationReply(NetPeer source, Message msg, TransportHeader transportHeader)
        //{
        //    var replyHeader = msg.GetHeader<EntityOperationReply>();
        //    if (replyHeader != null)
        //    {
        //        //if we have transport header at this point then it can be only because of routed request
        //        if (transportHeader != null)
        //        {
        //            NetPeer target;
        //            if (_connectedNodesByNodeId.TryGetValue(transportHeader.RoutedNodeId.Value, out target))
        //            {
        //                msg.RemoveHeader<TransportHeader>(); //its not needed anymore
        //                target.Send(msg);
        //            }
        //            else
        //                Log.Warn("Cant route reply to disconnected Node<Id:{0}>", transportHeader.RoutedNodeId.Value);
        //        }
        //        else
        //        {
        //            DispatchEntityOperationReply(replyHeader, msg);
        //        }
        //    }
        //}

        //protected override void OnNodeConnected(NetPeer source)
        //{
        //    base.OnNodeConnected(source);

        //    var clusterInfo = MessageFactory.New<UpdateClusterInfo>();
        //    clusterInfo.Descriptions = ConnectedNodesByNetId.Values.Select(x => x.Description).ToList();
        //    source.Send(clusterInfo);
        //    MessageFactory.Free(clusterInfo);
        //}

        //private async void OnUpdateClusterInfo(UpdateClusterInfo msg)
        //{
        //    Log.Info("OnConnectionRequest<Nodes in cluster:{0}>", msg.Descriptions.Count);

        //    foreach (var nodeDescription in msg.Descriptions)
        //        await Connect(nodeDescription.EndPoint);
        //}


        //protected void RouteEntityOperationRequest(NetPeer source, Message msg, TransportHeader transportHeader,
        //    NetPeer target)
        //{
        //    if (transportHeader != null)
        //        target.Send(msg);
        //    else
        //    {
        //        //support client scenario later
        //        //if (source.Description.Type == NodeType.Client)//use routing because only server nodes are connected all to all
        //        //    msg.AttachHeader(new TransportHeader(Id, source.Description.NodeId));
        //        //else
        //        msg.AttachHeader(new TransportHeader(source.Description.NodeId, null));
        //        target.Send(msg);
        //    }
        //}

        //protected void DispatchEntityOperationReply(EntityOperationReply replyHeader, Message msg)
        //{
        //    PendingOperation continuation;
        //    if (_pendingOperationsByRequestId.TryGetValue(replyHeader.RequestId, out continuation))
        //    {
        //        _pendingOperationsByRequestId.Remove(replyHeader.RequestId);
        //        if (msg.Id == (uint)NodeMessageId.InvalidEntityOperation)
        //            continuation.TCS.SetException(new InvalidEntityOperationException());
        //        else
        //            continuation.TCS.SetResult(msg);
        //    }
        //    else
        //        Log.Warn("Received Reply with requestId:{0}  with no continuation for it",
        //            replyHeader.RequestId);
        //}

        //protected async void DispatchEntityOperationRequest(NetPeer source, EntityOperationRequest requestHeader,
        //    TransportHeader transportHeader, NodeService entity, Message msg)
        //{
        //    Message reply;
        //    if (msg.Id == (uint)NodeMessageId.EntityDiscoveryRequest)
        //    {
        //        var discoveryReply = MessageFactory.New<EntityDiscoveryReply>();
        //        discoveryReply.Description = Description;
        //        reply = discoveryReply;
        //    }
        //    else
        //        reply = await Protocol.Dispatch(entity, msg);

        //    reply.AttachHeader(new EntityOperationReply(requestHeader.RequestId));
        //    if (transportHeader != null)
        //    {
        //        NetPeer target;
        //        if (_connectedNodesByNodeId.TryGetValue(transportHeader.SourceNodeId, out target))
        //        {
        //            if (transportHeader.RoutedNodeId.HasValue)
        //                reply.AttachHeader(transportHeader);
        //            reply.AttachHeader(new UpdateEntityRoutingHeader(entity.Id, Id));

        //            target.Send(reply);
        //        }
        //        else
        //            Log.Warn("Cant reply to disconnected Node<Id:{0}>", transportHeader.SourceNodeId);
        //    }
        //    else
        //        source.Send(reply);

        //    MessageFactory.Free(reply);
        //}

        //protected async Task<NodeService> ActivateEntity(ulong entityId)
        //{
        //    NodeService entity;
        //    if (Repository.TryGet(entityId, out entity))
        //        Log.Warn("{0} is already activated", entity);
        //    else
        //        entity = await Repository.Activate(entityId);

        //    return entity;
        //}

        //protected async Task DeleteEntity(ulong entityId)
        //{
        //    NodeService entity;
        //    if (Repository.TryGet(entityId, out entity))
        //        await Repository.Delete(entity);
        //    else
        //        Log.Warn("Cant delete Entity<Id:{0}> - not found", entityId);
        //}

        //public TEntityContract Get<TEntityContract>(uint entityId = 0, NetPeer target = null)
        //    where TEntityContract : class
        //{
        //    ulong fullId = Protocol.GetFullId<TEntityContract>(entityId);
        //    NodeServiceProxy proxy;
        //    if (!_proxyCache.TryGetValue(fullId, out proxy))
        //    {
        //        proxy = Protocol.CreateProxy(fullId);
        //        proxy.Node = this;
        //        proxy.Target = target;
        //        _proxyCache.Add(fullId, proxy);
        //    }
        //    return (TEntityContract)(object)proxy;
        //}

        //public override async Task<Message> Execute(Message input, NodeServiceProxy proxy)
        //{
        //    uint requestId = _requestId++;

        //    input.AttachHeader(new EntityOperationRequest(requestId, proxy.ServiceId));

        //    if (Repository.Contains(proxy.ServiceId))
        //        throw new NotImplementedException();
        //    else
        //    {
        //        NetPeer targetNode;
        //        //first check if we already know where this entity is located
        //        if (_entityRoutingTable.TryGetValue(proxy.ServiceId, out targetNode))
        //        {
        //            Log.Trace("Sending  to cached: " + targetNode);
        //            targetNode.Send(input);
        //        }
        //        //if we are master then choose where this entity will be created
        //        else if (Master == null)
        //        {
        //            targetNode = GetBalancedCreationTarget();
        //            if (targetNode == null)//we are the only server node
        //                throw new NotImplementedException();
        //            else
        //            {
        //                _entityRoutingTable.Add(proxy.ServiceId, targetNode);
        //                targetNode.Send(input);
        //            }
        //        }
        //        else
        //        {
        //            if (proxy.Description.Connectionfull)
        //            {
        //                targetNode = await ConnectToEntityOwner(proxy);
        //                targetNode.Send(input);
        //            }
        //            else
        //                Master.Send(input);
        //        }
        //    }
        //    Message reply = await AwaitEntityOperationReply(requestId, proxy);

        //    return reply;
        //}



        //protected NetPeer GetBalancedCreationTarget()
        //{
        //    var nodes = _connectedNodesByNodeId.Values.ToList();
        //    if (nodes.Count == 0)//we are solo so create entity on us
        //        return null;
        //    else
        //    {
        //        //change this in future to real loadbalancing
        //        var pos = _random.Next(nodes.Count);
        //        return nodes[pos];
        //    }
        //}

        //protected async Task<NetPeer> ConnectToEntityOwner(NodeServiceProxy proxy)
        //{
        //    Message reply = await Execute(new EntityDiscoveryRequest(), proxy);
        //    NetPeer target = await Connect(((EntityDiscoveryReply)reply).Description.EndPoint);
        //    return target;
        //}

        //public abstract async Task EntityCallAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;

        //public abstract async void EntityCallOnewayAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;
    }
}
