using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
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
        private ActionBlock<Issue> _writeLockedQueue;
        private ActionBlock<Issue> _readLockedQueue;

        public ServerFiber()
        {
            _issueQueue = new ActionBlock<Issue>((Func<Issue, Task>)ProcessIssue);
            CreateQueues();
        }

        private void CreateQueues()
        {
            _readLockedQueue = new ActionBlock<Issue>((Func<Issue, Task>)ProcessFunc,
                new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ConcurrentScheduler, MaxDegreeOfParallelism = 10});
            _writeLockedQueue = new ActionBlock<Issue>((Func<Issue, Task>)ProcessFunc,
                new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ExclusiveScheduler });
        }

        private async Task ProcessFunc(Issue issue)
        {
            await issue.Func();
        }

        private async Task ProcessIssue(Issue issue)
        {
            switch (issue.Lock)
            {
                case LockType.None:
                    issue.Func();
                    break;
                case LockType.ReadReentrant:
                    _readLockedQueue.Post(issue);
                    break;
                case LockType.WriteReentrant:
                    _writeLockedQueue.Post(issue);
                    break;
                case LockType.Full:
                    if (_readLockedQueue.InputCount > 0)
                    {
                        _readLockedQueue.Complete();
                        await _readLockedQueue.Completion;
                        _readLockedQueue = new ActionBlock<Issue>((Func<Issue, Task>)ProcessFunc,
                            new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ConcurrentScheduler });
                    }
                    if (_writeLockedQueue.InputCount > 0)
                    {
                        _writeLockedQueue.Complete();
                        await _writeLockedQueue.Completion;
                        _writeLockedQueue = new ActionBlock<Issue>((Func<Issue, Task>)ProcessFunc,
                            new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ExclusiveScheduler });
                    }
                    
                    await ProcessFunc(issue);
                    break;
            }
        }

        public void Process(Func<Task> func, LockType lockType)
        {
            _issueQueue.Post(new Issue(func, lockType));
        }

        public Task<TRetType> ProcessAndReturn<TRetType>(Func<Task<TRetType>> func, LockType lockType)
        {
            var tcs = new TaskCompletionSource<TRetType>();
            Func<Task> wrapFunc = async () =>
                {
                    try
                    {
                        var result = await func();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                };
            _issueQueue.Post(new Issue(wrapFunc, lockType));
            return tcs.Task;
        }
    }

    public class C2SPeer : NetPeer
    {
        private ConcurrentDictionary<uint, object> _handlersByNetContractId;
        
        private ServerFiber _fiber;
        public ServerFiber Fiber
        {
            get { return _fiber; }
        }

        public new IServerNode Node;

        public sealed override void Init(INetChannel channel, INode node)
        {
            base.Init(channel, node);

            _fiber = new ServerFiber();
            MessageEvent.Subscribe(OnClientMessage);
            _handlersByNetContractId = new ConcurrentDictionary<uint, object>();
            OnCreated();
        }

        public virtual void OnCreated()
        {
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
            Log.Debug("Received " + msg);
            try
            {
                if (msg is ServiceAccessRequest)
                {
                    var requestMsg = (msg as ServiceAccessRequest);
                    if (_handlersByNetContractId.ContainsKey(requestMsg.ServiceKey.TypeId))
                    {
                        var reply = new ServiceAccessReply(true, null);
                        reply.AttachHeader(new OperationHeader(requestMsg.GetHeader<OperationHeader>().RequestId, OperationType.Reply));
                        Channel.Send(reply);
                    }
                    else
                    {
                        var serviceDesc = Node.Protocol.GetDescription(requestMsg.ServiceKey.TypeId);
                        if (serviceDesc == null || !serviceDesc.AllowExternalConnections)
                        {
                            var reply = new ServiceAccessReply(false, null);
                            reply.AttachHeader(new OperationHeader(requestMsg.GetHeader<OperationHeader>().RequestId, OperationType.Reply));
                            Channel.Send(reply);
                        }
                        else
                            Node.ProcessServiceAccess(this, requestMsg);
                    }
                }
                else 
                {
                    var serviceHeader = msg.GetHeader<ServiceHeader>();
                    if (serviceHeader != null)
                    {
                        object handler;
                        if (_handlersByNetContractId.TryGetValue(serviceHeader.TargetServiceKey.TypeId, out handler))
                        {
                            Log.Debug("Dispatching {0} to client", msg);
                            if (msg.LockType == LockType.None)
                                DispatchAndReplyAsync(handler, msg);
                            else
                                Fiber.Process(() => DispatchAndReplyAsync(handler, msg), msg.LockType);
                        }
                        else
                        {
                            Log.Debug("Dispatching {0} to service", msg);
                            Node.DispatchClientOperationToService(serviceHeader.TargetServiceKey, msg, this);
                        }
                    }
                    else
                    {
                        Log.Warn("Received unprocessable {0}", msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString);
                Channel.Close();
            }
        }

        private async Task DispatchAndReplyAsync(object handler, Message msg)
        {
            OperationHeader operationHeader = msg.GetHeader<OperationHeader>();
            try
            {
                if (operationHeader != null)
                {
                    if(operationHeader.Type != OperationType.Request)
                    {
                        Log.Error("Received malformed service operation {0}", msg);
                        return;
                    }

                    Message reply = await Node.Protocol.Dispatch(handler, msg);
                    reply.AttachHeader(new OperationHeader(operationHeader.RequestId, OperationType.Reply));
                    Channel.Send(reply);
                    Log.Debug("Sending back {0}", reply);
                }
                else
                {
                    Node.Protocol.DispatchOneWay(handler, msg);
                }
            }
            catch (InvalidInput iex)
            {
                if (operationHeader != null)
                {
                    Log.Info(iex.Message);
                    var invalidReply = new InvalidOperation(iex.ErrorCode, iex.Message);
                    invalidReply.AttachHeader(new OperationHeader(operationHeader.RequestId, OperationType.Reply));
                    Channel.Send(invalidReply);
                }
                else
                {
                    Log.Warn("Dont use invalid input in oneWay operations, it wont be delivered anyway");
                }
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Error on processing {0} in {1}", msg, this), ex);
            }
        }
    }

    public class S2SPeer : NetPeer
    {
        public new IServerNode Node;

        public sealed override void Init(INetChannel channel, INode node)
        {
            base.Init(channel, node);

            MessageEvent.Subscribe(OnServerMessage);
        }

        private void OnServerMessage(Message msg)
        {
            Log.Debug("Received " + msg);
            try
            {
                var serviceHeader = msg.GetHeader<ServiceHeader>();
                if (serviceHeader != null)
                {
                    Log.Debug("Dispatching {0} to service", msg);
                    Node.DispatchServerOperationToService(serviceHeader.TargetServiceKey, msg, this);
                }
                else
                {
                    Log.Warn("Received unprocessable {0}", msg);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString);
                Channel.Close();
            }
        }

        private async Task DispatchAndReplyAsync(object handler, Message msg)
        {
            OperationHeader operationHeader = msg.GetHeader<OperationHeader>();
            try
            {
                if (operationHeader != null)
                {
                    if (operationHeader.Type != OperationType.Request)
                    {
                        Log.Error("Received malformed service operation {0}", msg);
                        return;
                    }

                    Message reply = await Node.Protocol.Dispatch(handler, msg);
                    reply.AttachHeader(new OperationHeader(operationHeader.RequestId, OperationType.Reply));
                    Channel.Send(reply);
                    Log.Debug("Sending back {0}", reply);
                }
                else
                {
                    Node.Protocol.DispatchOneWay(handler, msg);
                }
            }
            catch (InvalidInput iex)
            {
                if (operationHeader != null)
                {
                    Log.Info(iex.Message);
                    var invalidReply = new InvalidOperation(iex.ErrorCode, iex.Message);
                    invalidReply.AttachHeader(new OperationHeader(operationHeader.RequestId, OperationType.Reply));
                    Channel.Send(invalidReply);
                }
                else
                {
                    Log.Warn("Dont use invalid input in oneWay operations, it wont be delivered anyway");
                }
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Error on processing {0} in {1}", msg, this), ex);
            }
        }
    }

    public interface IServerNode : INode
    {
        Task<TNetContract> GetService<TNetContract>(uint serviceLocalId = 0);
        void ProcessServiceAccess(C2SPeer peer, ServiceAccessRequest request);
        void DispatchClientOperationToService(NodeServiceKey serviceKey, Message msg, C2SPeer clientPeer);
        void DispatchServerOperationToService(NodeServiceKey targetServiceKey, Message msg, S2SPeer serverPeer);
        INetNode<C2SPeer> ExternalNet { get; }
        INetNode<S2SPeer> InternalNet { get; }
    }

    public class ServerNode : IServerNode
    {
        private readonly ulong _id; 
        public readonly Logger Log;
        private readonly ConcurrentDictionary<NodeServiceKey, NodeServiceProxy> _proxyCache = new ConcurrentDictionary<NodeServiceKey, NodeServiceProxy>();
        //protected readonly Dictionary<ulong, NetPeer> _connectedNodesByNodeId = new Dictionary<ulong, NetPeer>();

        public INetNode<C2SPeer> ExternalNet { get; set; }
        public INetNode<S2SPeer> InternalNet { get; set; }

        private readonly Random _random = new Random();
        protected Random Rnd
        {
            get { return _random; }
        }

        public ulong Id
        {
            get { return _id; }
        }

        public IServicesRepository Repository { get; private set; }
        
        public IServiceProtocol Protocol { get; private set; }
        
        public IMessageFactory MessageFactory
        {
            get { return ExternalNet.MessageFactory; }
        }

        public ServerNode(INetProvider externalNetProvider, INetProvider internalNetProvider,
            IMessageFactory factory, IServiceProtocol protocol, IServicesRepository repository, C2SPeer clientPeerPrototype)
        {
            _id = GenerateUniqueId();
            Log = LogManager.GetLogger(ToString());
            Repository = repository;
            Protocol = protocol;
            Type clientPeerType = clientPeerPrototype.GetType();
            ExternalNet = new NetNode<C2SPeer>(externalNetProvider, factory, protocol,
                peerFactory: () =>
                             {
                                 var peer = (C2SPeer)FormatterServices.GetUninitializedObject(clientPeerType);
                                 peer.Node = this;
                                 return peer;
                             });
            InternalNet = new NetNode<S2SPeer>(internalNetProvider, factory, protocol);
        }

        public async Task<TNetContract> GetService<TNetContract>(uint serviceId = 0)
        {
            NodeServiceKey serviceKey = Protocol.GetKey<TNetContract>(serviceId);
            NodeServiceProxy proxy;
            
            if (_proxyCache.TryGetValue(serviceKey, out proxy))
                return (TNetContract)(object)proxy;

            NodeService service;
            if(!Repository.TryGet(serviceKey, out service))
            {
                service = await Repository.Activate(this, serviceKey);
            }
            return (TNetContract)(object)_proxyCache.GetOrAdd(serviceKey,
                (sk) => Protocol.CreateProxy(sk, MessageFactory, service));
        }

        public async void DispatchClientOperationToService(NodeServiceKey serviceKey, Message msg, C2SPeer clientPeer)
        {
            try
            {
                Log.Debug("Dispatching {0} from {1} {2}", msg, clientPeer, serviceKey);
                NodeServiceContractDescription desc = Protocol.GetDescription(serviceKey.TypeId);
                if (desc == null || !desc.AllowExternalConnections)
                {
                    Log.Debug("Dispatching {0} from {1} {2} : service is invalid or not visible", msg, clientPeer, serviceKey);
                    return;
                }

                NodeService service;
                if (Repository.TryGet(serviceKey, out service))
                {
                    Message reply = await service.ProcessMessage(new OperationContext(msg, clientPeer));
                    if (reply != null)
                    {
                        int requestId = msg.GetHeader<OperationHeader>().RequestId;
                        reply.AttachHeader(new OperationHeader(requestId, OperationType.Reply));
                        clientPeer.Channel.Send(reply);
                        Log.Debug("Dispatched {0} from {1} {2} : sending back reply {3}", msg, clientPeer, serviceKey, reply);
                    }
                }
                else
                    Log.Debug("Dispatching {0} from {1} {2} : Non active service", msg, clientPeer, serviceKey);
            }
            catch (InvalidInput iex)
            {
                Log.Info(iex.ToString());
                var invalidReply = new InvalidOperation(iex.ErrorCode, iex.Message);
                int requestId = msg.GetHeader<OperationHeader>().RequestId;
                invalidReply.AttachHeader(new OperationHeader(requestId, OperationType.Reply));
                clientPeer.Channel.Send(invalidReply);
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Error on Dispatching {0} from {1} to {2}", msg, clientPeer, serviceKey), ex);
            }
        }

        public async void DispatchServerOperationToService(NodeServiceKey serviceKey, Message msg, S2SPeer serverPeer)
        {
            try
            {
                Log.Debug("Dispatching {0} from {1} {2}", msg, serverPeer, serviceKey);
                NodeServiceContractDescription desc = Protocol.GetDescription(serviceKey.TypeId);
                
                NodeService service;
                if (Repository.TryGet(serviceKey, out service))
                {
                    Message reply = await service.ProcessMessage(new OperationContext(msg, serverPeer));
                    if (reply != null)
                    {
                        int requestId = msg.GetHeader<OperationHeader>().RequestId;
                        reply.AttachHeader(new OperationHeader(requestId, OperationType.Reply));
                        serverPeer.Channel.Send(reply);
                        Log.Debug("Dispatched {0} from {1} {2} : sending back reply {3}", msg, serverPeer, serviceKey, reply);
                    }
                }
                else
                    Log.Debug("Dispatching {0} from {1} {2} : Non active service", msg, serverPeer, serviceKey);
            }
            catch (InvalidInput iex)
            {
                Log.Info(iex.ToString());
                var invalidReply = new InvalidOperation(iex.ErrorCode, iex.Message);
                int requestId = msg.GetHeader<OperationHeader>().RequestId;
                invalidReply.AttachHeader(new OperationHeader(requestId, OperationType.Reply));
                serverPeer.Channel.Send(invalidReply);
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Error on Dispatching {0} from {1} to {2}", msg, serverPeer, serviceKey), ex);
            }
        }

        public void ProcessServiceAccess(C2SPeer peer, ServiceAccessRequest request)
        {
            Log.Debug("{0} is accessing service {1}", peer, request.ServiceKey);
            NodeServiceContractDescription desc = Protocol.GetDescription(request.ServiceKey.TypeId);
            bool allowed = false;
            NodeDescription connectTo = null;
            if (desc == null || !desc.AllowExternalConnections)
            {
                Log.Debug("{0} can't access service {1}", peer, request.ServiceKey);
            }
            else
            {
                //TODO: add cluster support here
                if (Repository.Contains(request.ServiceKey))
                {
                    allowed = true;
                    Log.Debug("{0} can access service {1}", peer, request.ServiceKey);
                }
            }

            var reply = new ServiceAccessReply(allowed, connectTo);
            reply.AttachHeader(new OperationHeader(request.GetHeader<OperationHeader>().RequestId, OperationType.Reply));
            peer.Channel.Send(reply);
        }

        private ulong GenerateUniqueId()
        {
            return ((ulong)Rnd.Next() << 32) ^ (ulong)Rnd.Next();
        }

        public override string ToString()
        {
            return string.Format("ServerNode<Id:{0}>", _id);
        }

        public void Start()
        {
            ExternalNet.Start();
        }

        public void Stop()
        {
            ExternalNet.Stop();
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
