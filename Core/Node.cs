using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NLog;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using System.IO;
using System.Diagnostics;

namespace MOUSE.Core
{
    public class PendingConnection
    {
        public readonly TaskCompletionSource<INetPeer> TCS;
        public readonly DateTime StartTime;
        public readonly IPEndPoint Target;
        public readonly CancellationTokenSource Expiration;

        public PendingConnection(IPEndPoint target)
        {
            TCS = new TaskCompletionSource<INetPeer>();
            StartTime = DateTime.Now;
            Target = target;
            Expiration = new CancellationTokenSource();
        }
    }

    public class PendingOperation
    {
        public readonly TaskCompletionSource<Message> TCS;
        public readonly DateTime StartTime;
        public readonly int RequestId;
        public readonly CancellationTokenSource Expiration;

        public PendingOperation(int requestId)
        {
            TCS = new TaskCompletionSource<Message>();
            StartTime = DateTime.Now;
            RequestId = requestId;
            Expiration = new CancellationTokenSource();
        }
    }

    public interface INetPeer : INetChannelListener
    {
        void Init(INetChannel channel, INode node);

        INetChannel Channel {get;}

        IObservable<INetPeer> DisconnectedEvent { get; }
        IObservable<Message> MessageEvent { get; }

        IMessageFactory MessageFactory { get; }

        Task<Message> ExecuteOperation(Message input);
        T As<T>();
    }

    public interface INode
    {
        IMessageFactory MessageFactory { get; }
        IServiceProtocol Protocol { get; }

        void Start();
        void Stop();
    }

    /// <summary>
    /// Different peers could simultaniously receive events, so implementation should be aware of this
    /// </summary>
    public interface INetNode<out TNetPeer> : INode
        where TNetPeer : INetPeer
    {
        /// <summary>
        /// should be called only for manually updated Nodes
        /// </summary>
        void Update();

        Task<INetPeer> Connect(IPEndPoint endPoint);//Why Task<T> is not covariant???

        IObservable<TNetPeer> PeerConnectedEvent { get; }
        IObservable<TNetPeer> PeerDisconnectedEvent { get; }

        IPEndPoint Endpoint { get; }
        
    }

    /// <summary>
    /// All fields should be initialized only in Init
    /// </summary>
    public class NetPeer : INetPeer, IServiceOperationDispatcher
    {
        private const int ExpirationTimeout = 30;

        public Logger Log;
        public INetChannel Channel { get; private set; }
        private Subject<INetPeer> _onDisconnectedSubject;
        private Subject<Message> _onMessageSubject;
        public INode Node { get; private set; }

        private int _requestId = 0;
        protected ConcurrentDictionary<int, PendingOperation> PendingOperationsByRequestId;
        private ConcurrentDictionary<NodeServiceKey, NodeServiceProxy> _proxyCache;

        public virtual void Init(INetChannel channel, INode node)
        {
            Channel = channel;
            Node = node;
            Log = LogManager.GetLogger(ToString());
            _onMessageSubject = new Subject<Message>();
            _onDisconnectedSubject = new Subject<INetPeer>();
            PendingOperationsByRequestId = new ConcurrentDictionary<int, PendingOperation>();
            _proxyCache = new ConcurrentDictionary<NodeServiceKey, NodeServiceProxy>();
        }

        void INetChannelListener.OnDisconnected()
        {
            _onDisconnectedSubject.OnNext(this);
            _onDisconnectedSubject.OnCompleted();
        }

        void INetChannelListener.OnNetData(BinaryReader reader)
        {
            Message msg = Node.MessageFactory.Deserialize(reader);
            var operationHeader = msg.GetHeader<OperationHeader>();
            if (operationHeader != null && operationHeader.Type == OperationType.Reply)
            {
                PendingOperation continuation;
                if (PendingOperationsByRequestId.TryRemove(operationHeader.RequestId, out continuation))
                {
                    continuation.Expiration.Cancel();
                    if (msg.Id == (uint)NodeMessageId.InvalidOperation)
                    {
                        var invMsg = msg as InvalidOperation;
                        continuation.TCS.SetException(new InvalidInput(invMsg.ErrorCode, invMsg.DebugDescription));
                    }
                    else
                        continuation.TCS.SetResult(msg);
                }
                else
                    Log.Warn("Received Reply with requestId:{0}  with no continuation for it", operationHeader.RequestId);
            }

            _onMessageSubject.OnNext(msg);
        }

        public IObservable<T> ReceiveMessage<T>() where T : Message
        {
            return MessageEvent.OfType<T>();
        }

        public IObservable<T> ReceiveMessage<T>(TimeSpan waitTime) where T : Message
        {
            var cancelation = new CancellationTokenSource();
            cancelation.CancelAfter(waitTime);
            return MessageEvent.OfType<T>().RunAsync(cancelation.Token);
        }

        public IMessageFactory MessageFactory
        {
            get { return Node.MessageFactory; }
        }

        public virtual Task<Message> ExecuteOperation(Message input)
        {
            int requestId = Interlocked.Increment(ref _requestId);
            input.AttachHeader(new OperationHeader(requestId, OperationType.Request));
            Channel.Send(input);

            var continuation = new PendingOperation(requestId);
            if (!PendingOperationsByRequestId.TryAdd(requestId, continuation))
                throw new Exception("This could happen only if requestId is duplicated");

            var expiration = Task.Delay(TimeSpan.FromSeconds(ExpirationTimeout), continuation.Expiration.Token);
            expiration.ContinueWith((_) =>
            {
                PendingOperation dummy;
                if (PendingOperationsByRequestId.TryRemove(continuation.RequestId, out dummy))
                    continuation.TCS.SetException(new Exception(string.Format("ExecuteOperation<{0}, {1}> has Expired after {2} sec",requestId, input, ExpirationTimeout)));
            });
            return continuation.TCS.Task;
        }

        public virtual T As<T>()
        {
            NodeServiceKey serviceKey = Node.Protocol.GetKey<T>();
            return (T)(object)_proxyCache.GetOrAdd(serviceKey, createProxy);
        }

        private NodeServiceProxy createProxy(NodeServiceKey serviceKey)
        {
            return Node.Protocol.CreateProxy(serviceKey, MessageFactory, this);
        }

        public IObservable<INetPeer> DisconnectedEvent
        {
            get { return _onDisconnectedSubject; }
        }

        public IObservable<Message> MessageEvent
        {
            get { return _onMessageSubject; }
        }

        public override string ToString()
        {
            return string.Format("NodeProxy<NetId:{0}, Endpoint:{1}>", Channel.Id, Channel.EndPoint);
        }

        Task<Message> IServiceOperationDispatcher.ExecuteServiceOperation(Message request)
        {
            return ExecuteOperation(request);
        }

        void IServiceOperationDispatcher.ExecuteOneWayServiceOperation(Message request)
        {
            Channel.Send(request);
        }
    }

    /// <summary>
    /// is thread safe
    /// </summary>
    public class NetNode<TNetPeer> : INetNode<TNetPeer>, INetChannelConsumer
        where TNetPeer : INetPeer
    {
        private const int MaxMessagesPerTick = 100000;
        private const int MaxEventsPerTick = 100000;
        private const int ExpirationTimeout = 10;

        public Logger Log;

        protected readonly ConcurrentDictionary<uint, TNetPeer> PeersByNetId
            = new ConcurrentDictionary<uint, TNetPeer>();
        private readonly ConcurrentDictionary<IPEndPoint, PendingConnection> _pendingConnections
            = new ConcurrentDictionary<IPEndPoint, PendingConnection>();

        private readonly bool _manualUpdate;
        private int _updateLoopRunning = 0;
        private Thread _updateThread;

        protected int RequestId = 0;

        public IMessageFactory MessageFactory { get; set; }
        public INetProvider Net { get; set; }
        public IServiceProtocol Protocol { get; set; }

        protected Func<TNetPeer> PeerFactory;

        public NetNode(INetProvider net, IMessageFactory msgFactory, IServiceProtocol protocol,
            bool manualUpdate = false, Func<TNetPeer> peerFactory = null)
        {
            Net = net;
            MessageFactory = msgFactory;
            Protocol = protocol;

            if (peerFactory != null)
                PeerFactory = peerFactory;
            else
            {
                PeerFactory = () => (TNetPeer)FormatterServices.GetUninitializedObject(typeof(TNetPeer));
            }

            _manualUpdate = manualUpdate;
        }

        public virtual void Start()
        {
            Log = LogManager.GetLogger(ToString());
            if (!Net.Init(this))
                throw new Exception("Net layer can't initialize");

            if (!_manualUpdate)
            {
                _updateLoopRunning = 1;
                _updateThread = new Thread(UpdateLoop);
                _updateThread.Start();
                Log.Info("UpdateThread started");
            }
        }

        public virtual void Stop()
        {
            Net.Shutdown();
            if (_updateLoopRunning == 1)
            {
                Interlocked.Exchange(ref _updateLoopRunning, 0);
                _updateThread.Join();
            }
        }

        Stopwatch _timer = new Stopwatch();
        public virtual void Update()
        {
            _timer.Restart();
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                if (!Net.PumpEvents())
                    break;
            }

            OnNodeUpdate();
            _timer.Stop();
            if (_timer.ElapsedMilliseconds > 100)
                Log.Warn("{0} Update took {1}ms", this, _timer.ElapsedMilliseconds);
        }

        private void UpdateLoop()
        {
            Log.Info("UpdateLoop started");
            while (_updateLoopRunning == 1)
            {
                Update();
                Thread.Sleep(1);
            }
            Log.Info("UpdateLoop stopped");
        }

        INetChannelListener INetChannelConsumer.OnNetConnect(INetChannel channel)
        {
            Log.Info("NetId:{0} has connected", channel.Id);
            TNetPeer peer = CreatePeer(channel);
            peer.DisconnectedEvent.Subscribe(OnPeerDisconnected);
            if (!PeersByNetId.TryAdd(peer.Channel.Id, peer))
                throw new Exception("NetId should increment monotonically or never called from different threads");

            //OnNodeConnected(node);

            PendingConnection continuation;
            if (_pendingConnections.TryRemove(channel.EndPoint, out continuation))
            {
                continuation.Expiration.Cancel();
                continuation.TCS.SetResult(peer);
            }
            _onNodeConnectedSubject.OnNext(peer);

            return peer;
        }

        void OnPeerDisconnected(INetPeer peer)
        {
            Log.Info("NetId:{0} has disconnected", peer.Channel.Id);
            TNetPeer savedPeer;
            if (PeersByNetId.TryRemove(peer.Channel.Id, out savedPeer))
            {
                if (!object.ReferenceEquals(savedPeer, peer))
                    throw new Exception("WTF! peer in dictionary is not the same as in disconnect event");
                Log.Info("{0} has disconnected", savedPeer);
                _onNodeDisconnectedSubject.OnNext(savedPeer);
                //OnNodeDisconnected(node);
            }
        }

        protected virtual void OnNodeUpdate()
        { }

        public virtual TNetPeer CreatePeer(INetChannel channel)
        {
            var peer = PeerFactory();
            peer.Init(channel, (INetNode<INetPeer>)this);
            return peer;
        }


        readonly Subject<TNetPeer> _onNodeConnectedSubject = new Subject<TNetPeer>();
        public IObservable<TNetPeer> PeerConnectedEvent
        {
            get { return _onNodeConnectedSubject; }
        }

        readonly Subject<TNetPeer> _onNodeDisconnectedSubject = new Subject<TNetPeer>();
        public IObservable<TNetPeer> PeerDisconnectedEvent
        {
            get { return _onNodeDisconnectedSubject; }
        }

        public Task<INetPeer> Connect(IPEndPoint endPoint)
        {
            //check if we are already connected to this endpoint
            foreach (var pair in PeersByNetId)
            {
                if (endPoint.Equals(pair.Value.Channel.EndPoint))
                    return Task.FromResult((INetPeer)pair.Value);
            }

            return _pendingConnections.AddOrUpdate(endPoint,
                        (key) =>
                        {
                            var val = new PendingConnection(key);
                            Log.Info("Connecting to " + endPoint);
                            Net.Connect(endPoint);

                            var expiration = Task.Delay(TimeSpan.FromSeconds(ExpirationTimeout),
                                                          val.Expiration.Token);
                            expiration.ContinueWith((_) =>
                                    {
                                        PendingConnection cont;
                                        if (_pendingConnections.TryRemove(key, out cont))
                                            cont.TCS.SetException(new Exception("Cant connect to -" + key));
                                    });
                            return val;
                        },
                        (key, val) => val
                 ).TCS.Task;
        }

        public IPEndPoint Endpoint
        {
            get { return Net.EndPoint; }
        }

        public override string ToString()
        {
            return "NetNode - " + Net.EndPoint;
        }
    }
}
