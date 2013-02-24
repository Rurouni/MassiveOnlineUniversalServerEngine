using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Reactive;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NLog;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;

namespace MOUSE.Core
{
    public interface INode
    {
        IMessageFactory MessageFactory { get; }
        IOperationDispatcher Dispatcher { get; }

        void Start();
        void Stop();
    }

    /// <summary>
    /// Different peers could simultaniously receive events from network level, so implementation should be aware of this
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

        TNetPeer GetPeer(uint localPeerId);

    }

    /// <summary>
    /// is thread safe
    /// </summary>
    public class NetNode<TNetPeer> : INetNode<TNetPeer>, INetChannelConsumer
        where TNetPeer : INetPeer
    {
        private const int MaxMessagesPerTick = 100000;
        private const int ExpirationTimeout = 10;

        public Logger Log;

        protected readonly ConcurrentDictionary<uint, TNetPeer> PeersByNetId
            = new ConcurrentDictionary<uint, TNetPeer>();
        private readonly ConcurrentDictionary<IPEndPoint, PendingConnection> _pendingConnections
            = new ConcurrentDictionary<IPEndPoint, PendingConnection>();

        private readonly bool _manualUpdate;
        private int _updateLoopRunning = 0;
        private Thread _updateThread;

        readonly Stopwatch _timer = new Stopwatch();

        protected int RequestId = 0;

        public IMessageFactory MessageFactory { get; set; }
        public INetProvider Net { get; set; }
        public IOperationDispatcher Dispatcher { get; set; }

        protected Func<TNetPeer> PeerFactory;

        public NetNode(INetProvider net, IMessageFactory msgFactory, IOperationDispatcher protocol,
            bool manualUpdate = false, Func<TNetPeer> peerFactory = null)
        {
            Net = net;
            MessageFactory = msgFactory;
            Dispatcher = protocol;

            if (peerFactory != null)
                PeerFactory = peerFactory;
            else
            {
                PeerFactory = () => (TNetPeer)Activator.CreateInstance(typeof(TNetPeer));
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
            if (_updateLoopRunning == 1)
            {
                Interlocked.Exchange(ref _updateLoopRunning, 0);
                _updateThread.Join();
            }
            Net.Shutdown();
        }

        
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
            peer.Init(channel, this);
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
                                            cont.TCS.SetException(new ConnectionFailedException(key));
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

        public TNetPeer GetPeer(uint localPeerId)
        {
            TNetPeer peer;
            if (PeersByNetId.TryGetValue(localPeerId, out peer))
                return peer;
            else
                return default(TNetPeer);
        }

        public override string ToString()
        {
            return "NetNode - " + Net.EndPoint;
        }
    }

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
}
