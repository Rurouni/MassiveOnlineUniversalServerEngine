using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace MOUSE.Core
{
    public interface IClientNode
    {
        Task ConnectToServer(IPEndPoint serverEndPoint);
        Task<TNetContract> GetService<TNetContract>(uint serviceLocalId = 0);
        void SetHandler<TNetContract>(TNetContract implementer);
        IObservable<IClientNode> DisconnectedEvent { get; }
    }


    public class ClientFiber
    {
        readonly ActionBlock<Action> _processingQueue;
        private readonly bool _manualUpdate;
        readonly ConcurrentQueue<Action> _manualProcessingQueue = new ConcurrentQueue<Action>();

        public ClientFiber(TaskScheduler scheduler, bool manualUpdate = false)
        {
            _processingQueue = new ActionBlock<Action>((func) => func(),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = scheduler
                });
            _manualUpdate = manualUpdate;
        }

        public ClientFiber(bool manualUpdate = false)
            : this(TaskScheduler.Default, manualUpdate)
        {
        }


        public void Process(Action func)
        {
            if (_manualUpdate)
                _manualProcessingQueue.Enqueue(func);
            else
                _processingQueue.Post(func);
        }

        public Task ContinueOn()
        {
            var tcs = new TaskCompletionSource<object>();
            Process(() => tcs.SetResult(null));
            return tcs.Task;
        }

        public Task<T> Process<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Process(async() =>
            {
                try
                {
                    T result = await func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        public Task<T> Process<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Process(() =>
            {
                try
                {
                    T result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        public void ExecuteAllInplace()
        {
            if (!_manualUpdate)
                return;
            Action action;
            int limit = 0;
            while (limit++ < 10000 && _manualProcessingQueue.TryDequeue(out action))
                action();
        }
    }

    public class Client2ServerPeer : NetPeer
    {
        public Client2ServerPeer(INetChannel channel, INetNode<INetPeer> owner)
            : base(channel, owner)
        {
        }

        public async override Task<Message> ExecuteOperation(Message input)
        {
            Message msg = await base.ExecuteOperation(input);
            await ((ClientNode)Owner).Fiber.ContinueOn().ConfigureAwait(false);
            return msg;
        }

    }
   
    /// <summary>
    /// uses internal Fiber to receive all continuations and process messages to achieve thread-safety and provide manual update loop(if needed)
    /// </summary>
    public class ClientNode : NetNode<Client2ServerPeer>, IClientNode
    {
        private readonly Dictionary<uint, object> _handlersByNetContractId = new Dictionary<uint, object>();
        private readonly Dictionary<ulong, NodeServiceProxy> _proxyCache = new Dictionary<ulong, NodeServiceProxy>();

        protected IPEndPoint ServerEndPoint;
        protected NetPeer ServerPeer;

        public ClientFiber Fiber;
        
        public ClientNode(INetProvider net, IMessageFactory msgFactory, IServiceProtocol protocol,
            bool manualUpdate = false, IPEndPoint serverEndpoint = null)
            : base(net, msgFactory, protocol, null, manualUpdate)
        {
            ServerEndPoint = serverEndpoint;
            if (SynchronizationContext.Current != null)
                Fiber = new ClientFiber(TaskScheduler.FromCurrentSynchronizationContext(), manualUpdate);
            else
                Fiber = new ClientFiber(manualUpdate);
        }

        protected override void OnNodeUpdate()
        {
            base.OnNodeUpdate();
            Fiber.ExecuteAllInplace();
        }

        public async Task ConnectToServer(IPEndPoint endPoint)
        {
            if (ServerPeer.Channel.EndPoint == endPoint)
                return;

            if (ServerPeer != null)
                ServerPeer.Channel.Close();

            ServerEndPoint = endPoint;
            ServerPeer = (NetPeer)await Connect(endPoint).ConfigureAwait(false);
        }

        public override Client2ServerPeer CreatePeer(INetChannel channel)
        {
            var peer = new Client2ServerPeer(channel, this);
            peer.MessageEvent.Subscribe((msg)=> Fiber.Process(() => OnMessage(msg)));
            return peer;
        }

        public IObservable<IClientNode> DisconnectedEvent
        {
            get
            {
                return from peer in PeerDisconnectedEvent
                       where peer.Channel.EndPoint == ServerEndPoint
                       select this;
            }
        }

        protected void OnMessage(Message msg)
        {
            var serviceHeader = msg.GetHeader<ServiceHeader>();
            var operationHeader = msg.GetHeader<OperationHeader>();
            if (serviceHeader != null && operationHeader.Type == OperationType.Request)
            {
                uint serviceContractId = Protocol.GetContractId(serviceHeader.TargetServiceKey);
                object handler;
                if (_handlersByNetContractId.TryGetValue(serviceContractId, out handler))
                {
                    //NOTE: doesnt support server->client request-reply, only one way notifications
                    Protocol.Dispatch(handler, msg);
                }
            }
        }

        public async Task<TNetContract> GetService<TNetContract>(uint serviceLocalId = 0)
        {
            await Fiber.ContinueOn().ConfigureAwait(false);

            if (ServerPeer == null)
            {
                if (ServerEndPoint != null)
                    ServerPeer = (NetPeer)await base.Connect(ServerEndPoint).ConfigureAwait(false);
                else
                    throw new Exception("Client node must be explicitly connected or masterEndpoint specified in constructor");
            }

            ulong fullId = Protocol.GetFullId<TNetContract>(serviceLocalId);
            NodeServiceProxy proxy;
            if (!_proxyCache.TryGetValue(fullId, out proxy))
            {
                var reply = (ServiceAccessReply)await ServerPeer.ExecuteOperation(new ServiceAccessRequest(fullId)).ConfigureAwait(false);

                if (!reply.IsValid)
                    throw new Exception("Invalid Access");

                NetPeer serviceOwnerNode;

                if (reply.ServiceOwner != null)
                    serviceOwnerNode = (NetPeer)await base.Connect(reply.ServiceOwner.EndPoint).ConfigureAwait(false);
                else
                    serviceOwnerNode = ServerPeer;

                proxy = Protocol.CreateProxy(fullId, serviceOwnerNode);
                await Fiber.ContinueOn().ConfigureAwait(false);
                _proxyCache.Add(fullId, proxy);
            }

            return (TNetContract)(object)proxy;
        }

        public void SetHandler<TNetContract>(TNetContract implementer)
        {
            Fiber.Process(() =>
            {
                _handlersByNetContractId[Protocol.GetContractId(typeof(TNetContract))] = implementer;
            });
        }
    }
}
