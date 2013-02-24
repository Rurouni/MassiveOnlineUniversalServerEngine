using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Reactive.Linq;
using MOUSE.Core.ActorCoordination;
using MOUSE.Core.Actors;
using MOUSE.Core.NodeCoordination;
using NLog;
using Isis;
using System.IO;

namespace MOUSE.Core
{
    public class NetContractHandler
    {
        public readonly uint ServiceTypeId;
        public readonly object Implementer;
        readonly Dictionary<uint, NetOperationHandlerAttribute> _handlerAttributes = new Dictionary<uint, NetOperationHandlerAttribute>();

        public NetContractHandler(IOperationDispatcher dispatcher, uint contractId, object implementer)
        {
            Implementer = implementer;
            ServiceTypeId = contractId;
            Type implementerType = implementer.GetType();
            NetContractDescription desc = dispatcher.GetContract(contractId);
            InterfaceMapping mapping = implementerType.GetInterfaceMap(desc.ContractType);
            
            foreach (NetOperationDescription operation in desc.Operations)
            {
                var handlerAttr = mapping.TargetMethods.First(m => m.Name == operation.Name).GetAttribute<NetOperationHandlerAttribute>();
                _handlerAttributes.Add(operation.RequestMessageId, handlerAttr ?? new NetOperationHandlerAttribute());
            }
        }


        public LockType GetLockTypeForOperation(Message msg)
        {
            return _handlerAttributes[msg.Id].Lock;
        }
    }

    public class S2CPeer : NetPeer
    {
        private ConcurrentDictionary<uint, NetContractHandler> _handlersByNetContractId;
        
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
            _handlersByNetContractId = new ConcurrentDictionary<uint, NetContractHandler>();
            OnCreated();
        }

        public virtual void OnCreated()
        {
        }

        protected void SetHandler<TNetContract>(TNetContract implementer)
        {
            uint contractId = Node.Dispatcher.GetContractId(typeof (TNetContract));
            _handlersByNetContractId[contractId] = new NetContractHandler(Node.Dispatcher, contractId, implementer);
        }

        protected void RemoveHandler<TNetContract>()
        {
            NetContractHandler removed;
            _handlersByNetContractId.TryRemove(Node.Dispatcher.GetContractId(typeof(TNetContract)), out removed);
        }

        private void OnClientMessage(Message msg)
        {
            Log.Debug("Received " + msg);
            try
            {
                NetContractDescription contractDesc = Node.Dispatcher.GetContractForMessage(msg.Id);

                var actorHeader = msg.GetHeader<ActorHeader>();
                if (actorHeader != null)
                {
                    Node.DispatchOperationToActor(actorHeader.ActorLocalId, msg, this, true);
                }
                else //by default operations are handled by client associated peer
                {
                    NetContractHandler handler;
                    if (_handlersByNetContractId.TryGetValue(contractDesc.TypeId, out handler))
                    {
                        LockType lockType = handler.GetLockTypeForOperation(msg);
                        Log.Debug("Dispatching {0} to client peer", msg);
                        if (lockType == LockType.None)
#pragma warning disable 4014
                            //for this lock level we really don't wait for any results
                            DispatchAndReplyAsync(handler.Implementer, msg);
#pragma warning restore 4014
                        else
                            Fiber.ProcessAsync(() => DispatchAndReplyAsync(handler.Implementer, msg), lockType);
                    }
                    else
                    {
                        Log.Debug("Skipping {0} because no handler is present", msg);
                        MessageFactory.Free(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageFactory.Free(msg);
                Log.Error("Channel will be closed after: " + ex);
                Channel.Close();
            }
        }

        private async Task DispatchAndReplyAsync(object handler, Message msg)
        {
            var operationHeader = msg.GetHeader<OperationHeader>();
            try
            {
                if (operationHeader != null)
                {
                    if (operationHeader.Type != OperationType.Request)
                    {
                        Log.Error("Received malformed service operation {0}", msg);
                        return;
                    }

                    Message replyMsg = await Node.Dispatcher.Dispatch(handler, msg);
                    Reply(msg, replyMsg);
                    Log.Debug("Sending back {0}", replyMsg);
                }
                else
                {
                    Node.Dispatcher.DispatchOneWay(handler, msg);
                }
            }
            catch (InvalidInput iex)
            {
                if (operationHeader != null)
                {
                    Log.Info(iex.Message);
                    ReplyWithError(msg, iex.ErrorCode, iex.Message);
                }
                else
                {
                    Log.Warn("Dont use invalid input in oneWay operations, it won't be delivered anyway");
                }
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Error on processing {0} in {1}", msg, this), ex);
            }
            finally
            {
                MessageFactory.Free(msg);
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
                var actorHeader = msg.GetHeader<ActorHeader>();
                if (actorHeader != null)
                {
                    Log.Debug("Dispatching {0} to service", msg);
                    Node.DispatchOperationToActor(actorHeader.ActorLocalId, msg, this, false);
                }
                else
                {
                    Log.Warn("Received {0} without actor header, skip it", msg);
                    MessageFactory.Free(msg);
                }
            }
            catch (Exception ex)
            {
                MessageFactory.Free(msg);
                Log.Error("Channel will be closed after: " + ex);
                Channel.Close();
            }
        }
    }

    public interface IServerNode : INode
    {
        NodeRemoteInfo GetNode(ulong nodeId);
        Task<S2SPeer> GetNodePeer(ulong nodeId);

        Task<ActorProxy<TPrimaryNetContract>> GetActor<TPrimaryNetContract>(ActorKey actorKey);
        Task<ActorProxy<TPrimaryNetContract>> GetActor<TPrimaryNetContract>(string actorName = "Singleton");
        Task<ActorProxy<TPrimaryNetContract>> GetActor<TPrimaryNetContract>(object routingKey);

        Task<TSecondaryNetContract> GetActorAs<TPrimaryNetContract, TSecondaryNetContract>(ActorKey actorKey);
        Task<TSecondaryNetContract> GetActorAs<TPrimaryNetContract, TSecondaryNetContract>(string actorName = "Singleton");
        Task<TSecondaryNetContract> GetActorAs<TPrimaryNetContract, TSecondaryNetContract>(object routingKey);

        IActorCoordinator GetCoordinator<TNetContract>();

        Task DispatchOperationToActor(uint actorLocalId, Message msg, NetPeer peer, bool externalCall);

        INetNode<S2CPeer> ExternalNet { get; }
        INetNode<S2SPeer> InternalNet { get; }

        IActorRepository Repository { get; }
        INodeCoordinator Coordinator { get; }
        ServerFiber Fiber { get; }
        ClusterView ClusterView { get; }
        
        ulong Id { get; set; } //only coordinator should set Id
        NodeRemoteInfo Info { get; }
        Random Rnd { get; }
    }

    public class ServerNode : IServerNode
    {
        public Logger Log;

        private readonly ConcurrentDictionary<ActorProxyKey, object> _proxyCache = new ConcurrentDictionary<ActorProxyKey, object>();
        private readonly Dictionary<uint, IActorCoordinator> _actorCoordinatorsByPrimaryContractId = new Dictionary<uint, IActorCoordinator>();


        public INetNode<S2CPeer> ExternalNet { get; set; }
        public INetNode<S2SPeer> InternalNet { get; set; }

        private readonly INodeCoordinator _coordinator;
        public INodeCoordinator Coordinator
        {
            get { return _coordinator; }
        }

        public ServerFiber Fiber { get; private set; }
        
        private readonly Random _random = new Random();
        public Random Rnd
        {
            get { return _random; }
        }

        private ulong _id; 
        public ulong Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private NodeRemoteInfo _info;
        public NodeRemoteInfo Info
        {
            get { return _info; }
        }

        public NodeRemoteInfo GetNode(ulong nodeId)
        {
            if (nodeId == Id)
                return _info;
            else
            {
                return ClusterView.Members.Find(x => x.NodeId == nodeId);
            }
        }

        public ClusterView ClusterView { get; private set; }

        public IActorRepository Repository { get; private set; }
        public Type DefaultActorCoordinator { get; set; }

        public IOperationDispatcher Dispatcher { get; private set; }
        
        public IMessageFactory MessageFactory
        {
            get { return ExternalNet.MessageFactory; }
        }

        public ServerNode(INetProvider externalNetProvider, INetProvider internalNetProvider, INodeCoordinator coordinator,
            IMessageFactory factory, IOperationDispatcher protocol, IActorRepository repository,
            Func<S2CPeer> clientPeerFactory)
        {
            _coordinator = coordinator;
            Repository = repository;
            Dispatcher = protocol;
            DefaultActorCoordinator = typeof(IsisActorCoordinator);
            ExternalNet = new NetNode<S2CPeer>(externalNetProvider, factory, protocol,
                peerFactory: () =>
                    {
                        S2CPeer peer = clientPeerFactory();
                        peer.Node = this;
                        return peer;
                    });
            InternalNet = new NetNode<S2SPeer>(internalNetProvider, factory, protocol, 
                peerFactory: () => new S2SPeer { Node = this } );

            foreach (ActorDescription actorDesc in repository.ActorDescriptions)
            {
                Type coordinatorType = actorDesc.Attribute.Coordinator ?? DefaultActorCoordinator;
                var actorCoord = (IActorCoordinator)Activator.CreateInstance(coordinatorType);
                actorCoord.Init(this, actorDesc.PrimaryContract.TypeId);
                _actorCoordinatorsByPrimaryContractId.Add(actorDesc.PrimaryContract.TypeId, actorCoord);
            }
            
            Fiber = new ServerFiber();
        }

        public void RegisterActorCoordinatorFor<TPrimaryNetContract>(IActorCoordinator coordinator)
        {
            NetContractDescription desc = Dispatcher.GetContract<TPrimaryNetContract>();
            coordinator.Init(this, desc.TypeId);
            _actorCoordinatorsByPrimaryContractId.Add(desc.TypeId, coordinator);
        }


        public async Task<ActorProxy<TPrimaryNetContract>> GetActor<TPrimaryNetContract>(ActorKey actorKey)
        {
            NetContractDescription desc = Dispatcher.GetContract<TPrimaryNetContract>();
            var proxyKey = new ActorProxyKey(actorKey, desc.TypeId);

            object proxy;
            if (_proxyCache.TryGetValue(proxyKey, out proxy))
                return (ActorProxy<TPrimaryNetContract>)proxy;

            IOperationExecutor operationExecutor;
            if (actorKey.OwnerNodeId == Id) //local actor
            {
                Actor actor;
                Repository.TryGet(actorKey.LocalActorId, out actor);
                operationExecutor = actor;
            }
            else
            {
                operationExecutor = await GetNodePeer(actorKey.OwnerNodeId);
            }

            return (ActorProxy<TPrimaryNetContract>)_proxyCache.GetOrAdd(proxyKey,
                (actorId) => new ActorProxy<TPrimaryNetContract>(
                                (TPrimaryNetContract)(object)Dispatcher.CreateProxy(Dispatcher.GetContractId(typeof(TPrimaryNetContract)), MessageFactory, operationExecutor, actorKey),
                                actorKey));
        }

        public async Task<ActorProxy<TPrimaryNetContract>> GetActor<TPrimaryNetContract>(string actorName)
        {
            IActorCoordinator coordinator = GetCoordinator<TPrimaryNetContract>();
            
            ActorKey actorKey = await coordinator.GetOrCreate(actorName);

            return await GetActor<TPrimaryNetContract>(actorKey);
        }

        public async Task<ActorProxy<TPrimaryNetContract>> GetActor<TPrimaryNetContract>(object routingKey)
        {
            throw new NotImplementedException();
        }

        public Task<TSecondaryNetContract> GetActorAs<TPrimaryNetContract, TSecondaryNetContract>(string actorName)
        {
            throw new NotImplementedException();
        }

        public Task<TSecondaryNetContract> GetActorAs<TPrimaryNetContract, TSecondaryNetContract>(ActorKey actorKey)
        {
            throw new NotImplementedException();
        }

        public Task<TSecondaryNetContract> GetActorAs<TPrimaryNetContract, TSecondaryNetContract>(object routingKey)
        {
            throw new NotImplementedException();
        }

        public IActorCoordinator GetCoordinator<TPrimaryNetContract>()
        {
            NetContractDescription desc = Dispatcher.GetContract<TPrimaryNetContract>();
            if (!desc.IsPrimary)
                throw new Exception("You can access Coordinator or Actor only by Primary contract");

            IActorCoordinator coordinator;
            if (!_actorCoordinatorsByPrimaryContractId.TryGetValue(desc.TypeId, out coordinator))
                throw new Exception("Can't find coordinator for actor group with primary contract " + desc.ContractType.Name);

            return coordinator;
        }

        public async Task<S2SPeer> GetNodePeer(ulong nodeId)
        {
            NodeRemoteInfo info = ClusterView.GetNode(nodeId);
            if (info.LocalConnectionId.HasValue)
            {
                return InternalNet.GetPeer(info.LocalConnectionId.Value);
            }
            else
            {
                var peer = (S2SPeer)await InternalNet.Connect(info.InternalEndpoint);
                info.LocalConnectionId = peer.Channel.Id;
                return peer;
            }
        }

        public async Task DispatchOperationToActor(uint actorLocalId, Message msg, NetPeer peer, bool externalCall)
        {
            try
            {
                Log.Debug("Dispatching {0} from {1} {2}", msg, peer, actorLocalId);
                NetContractDescription desc = Dispatcher.GetContractForMessage(msg.Id);
                if (desc == null)
                {
                    Log.Debug("Dispatching {0} from {1} {2} : contract unknown", msg, peer, actorLocalId);
                    return;
                }
                if (externalCall && !desc.AllowExternalConnections)
                {
                    Log.Debug("Dispatching {0} from {1} {2} : this actor doesn't allow external calls", msg, peer, actorLocalId);
                    return;
                }

                Actor actor;
                if (Repository.TryGet(actorLocalId, out actor))
                {
                    if (msg.GetHeader<OperationHeader>() != null)
                    {
                        Message reply = await ((IOperationExecutor) actor).ExecuteOperation(new OperationContext(msg, peer));
                        peer.Reply(msg, reply);

                        Log.Debug("Dispatched {0} from {1} {2} : sending back reply {3}", msg, peer, actorLocalId, reply);
                    }
                    else
                    {
                        ((IOperationExecutor) actor).ExecuteOneWayOperation(new OperationContext(msg, peer));
                    }
                }
                else
                {
                    peer.ReplyWithError(msg, (ushort) BasicErrorCode.NonExistentActor, "Actor not present on this node");
                    Log.Debug("Dispatching {0} from {1} {2} : actor does not exist", msg, peer, actorLocalId);

                }
            }
            catch (InvalidInput iex)
            {
                Log.Info(iex.ToString());
                peer.ReplyWithError(msg, iex.ErrorCode, iex.Message);
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Error on Dispatching {0} from {1} to {2}", msg, peer, actorLocalId), ex);
            }
            finally
            {
                MessageFactory.Free(msg);
            }
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
            _id = _coordinator.GenerateNodeId();
            _info = new NodeRemoteInfo(_id, InternalNet.Endpoint, ExternalNet.Endpoint);
            Log = LogManager.GetLogger(ToString());

            _coordinator.OnClusterViewChanged.Subscribe(ClusterViewChanged);
            _coordinator.JoinWith(this);

            ExternalNet.Start();
            InternalNet.Start();
        }

        private void ClusterViewChanged(ClusterView view)
        {
            Log.Info("New Cluster View: \n\t {0}", view);
            ClusterView = view;
        }


        public void Stop()
        {
            ExternalNet.Stop();
            InternalNet.Stop();
        }
    }
}
