using System;
using System.CodeDom;
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
using MOUSE.Core.Actors;
using Microsoft.CSharp;
using System.Threading.Tasks.Dataflow;

namespace MOUSE.Core
{
    public interface IClientNode
    {
        Task<IServerPeer> ConnectToServer(IPEndPoint serverEndPoint);
        Task<IServerPeer> ConnectToServer(string serverEndPoint);
        void Start();
        void Stop();
    }

    public interface IServerPeer : INetPeer
    {
        Task<TNetContract> GetProxy<TNetContract>(uint? localActorId = null);
        void SetHandler<TNetContract>(TNetContract implementer);
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

    /// <summary>
    /// All server peer's share same fiber from client node
    /// </summary>
    public class ServerPeer : NetPeer, IServerPeer
    {
        private Dictionary<uint, object> _handlersByNetContractId;
        private Dictionary<ActorProxyKey, NetProxy> _proxyCache;

        public new ClientNode Node { get { return (ClientNode)base.Node; } }

        public override void Init(INetChannel channel, INode node)
        {
            base.Init(channel, node);
            _handlersByNetContractId = new Dictionary<uint, object>();
            _proxyCache = new Dictionary<ActorProxyKey, NetProxy>();
            MessageEvent.Subscribe((msg) => Node.Fiber.Process(() => OnMessage(msg)));
        }

        public async Task<TNetContract> GetProxy<TNetContract>(uint? localActorId = null)
        {
            await Node.Fiber.ContinueOn().ConfigureAwait(false);

            NetContractDescription contractDesc = Node.Dispatcher.GetContract<TNetContract>();
            var proxyKey = new ActorProxyKey(new ActorKey(0, localActorId ?? 0), contractDesc.TypeId);
            NetProxy proxy;
            if (!_proxyCache.TryGetValue(proxyKey, out proxy))
            {
                proxy = Node.Dispatcher.CreateProxy(contractDesc.TypeId, MessageFactory, this, localActorId.HasValue ? proxyKey.ActorKey : (ActorKey?)null);
                _proxyCache.Add(proxyKey, proxy);
            }

            return (TNetContract)(object)proxy;
        }

        protected void OnMessage(Message msg)
        {
            NetContractDescription contractDesc = Node.Dispatcher.GetContractForMessage(msg.Id);
            object handler;
            if (_handlersByNetContractId.TryGetValue(contractDesc.TypeId, out handler))
            {
                //NOTE: doesnt support server->client request-reply, only one way notifications
                Node.Dispatcher.Dispatch(handler, msg);
            }
            else
                Log.Warn("Handler for {0} is unregistered", msg);
        }

        public void SetHandler<TNetContract>(TNetContract implementer)
        {
            Node.Fiber.Process(() =>
            {
                _handlersByNetContractId[Node.Dispatcher.GetContractId(typeof(TNetContract))] = implementer;
            });
        }
    }
   
    /// <summary>
    /// uses internal Fiber to receive all continuations and process messages to achieve thread-safety and provide manual update loop(if needed)
    /// </summary>
    public class ClientNode : NetNode<ServerPeer>, IClientNode
    {
        public ClientFiber Fiber;
        
        public ClientNode(INetProvider net, IMessageFactory msgFactory, IOperationDispatcher protocol,
            bool manualUpdate = false)
            : base(net, msgFactory, protocol, manualUpdate)
        {
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

        public async Task<IServerPeer> ConnectToServer(IPEndPoint endPoint)
        {
            return (IServerPeer)await Connect(endPoint).ConfigureAwait(false);
        }

        public async Task<IServerPeer> ConnectToServer(string serverEndPoint)
        {
            return (IServerPeer)await Connect(NodeRemoteInfo.ParseIPEndPoint(serverEndPoint)).ConfigureAwait(false);
        }
    }
}
