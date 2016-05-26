using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using MOUSE.Core.Configuration;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Serialization;
using Timer = Metrics.Timer;

namespace MOUSE.Core
{
    /// <summary>
    /// is thread safe
    /// </summary>
    public class NetNode<TNetChannel> : INetNode
        where TNetChannel : INetChannel
    {
        readonly Meter _netChannelsConnectedMeter;
        readonly Meter _netChannelsDisconnectedMeter;
        readonly Counter _netChannelsCounter;
        readonly Timer _nodeUpdateTimer;


        protected readonly ConcurrentDictionary<uint, TNetChannel> ChannelsById = new ConcurrentDictionary<uint, TNetChannel>();
        readonly ConcurrentDictionary<IPEndPoint, PendingConnection> _pendingConnections = new ConcurrentDictionary<IPEndPoint, PendingConnection>();

        readonly INetNodeConfig _config;
        readonly IPEndPoint _publicAddress;

        readonly ManualResetEventSlim _stopUpdateLoopSignal =  new ManualResetEventSlim(false);
        Thread _updateThread;
        readonly ICoreEvents _logger;

        protected int RequestId = 0;

        public string InstanceName { get; }
        public INetProvider Net { get; }
        public IMessageSerializer MessageSerializer { get; }

        protected Func<INetNode, ITransportChannel, TNetChannel> ChannelFactory;

        readonly Subject<TNetChannel> _onNodeConnectedSubject = new Subject<TNetChannel>();
        IObservable<INetChannel> INetNode.ChannelOpenedEvent
        {
            get { return _onNodeConnectedSubject.Select(x => (INetChannel)x); }
        }

        public IObservable<TNetChannel> ChannelOpenedEvent => _onNodeConnectedSubject;

        readonly Subject<TNetChannel> _onNodeDisconnectedSubject = new Subject<TNetChannel>();
        IObservable<INetChannel> INetNode.ChannelClosedEvent
        {
            get { return _onNodeDisconnectedSubject.Select(x => (INetChannel)x); }
        }

        public IObservable<TNetChannel> ChannelClosedEvent => _onNodeDisconnectedSubject;

        public IPEndPoint Address => _publicAddress??Net.EndPoint;

        public INetNodeConfig Config => _config;

        public ICoreEvents Logger => _logger;

        public IEnumerable<INetChannel> OpenedChannels => ChannelsById.Values.Cast<INetChannel>();

        public NetNode(string name, INetProvider net, ICoreEvents logger, IMessageSerializer messageSerializer, Func<INetNode, ITransportChannel, TNetChannel> channelFactory, INetNodeConfig config,
            IPEndPoint publicAddress = null)
        {
            _config = config;
            _publicAddress = publicAddress;
            _logger = logger;
            InstanceName = name;
            Net = net;
            MessageSerializer = messageSerializer;

            ChannelFactory = channelFactory;

            var metricsContext = Metric.Context(name);

            _netChannelsConnectedMeter = metricsContext.Meter("NetChannelsConnected", Unit.Custom("Channels"));
            _netChannelsDisconnectedMeter = metricsContext.Meter("NetChannelsDisconnected", Unit.Custom("Channels"));
            _netChannelsCounter = metricsContext.Counter("NetChannelsCount", Unit.Items);
            _nodeUpdateTimer = metricsContext.Timer("NodeUpdateTime", Unit.None);
        }

        public virtual void Start()
        {
            if (!Net.Init(ProcessNewConnection))
                throw new Exception("Network layer can't initialize");

            if(_publicAddress != null && _publicAddress.Port == 0)
                _publicAddress.Port = Net.EndPoint.Port;

            _logger.NodeStarted(this);

            if (!_config.ManualUpdateOnly)
            {
                _updateThread = new Thread(UpdateLoop);
                _updateThread.Start();
                _logger.NodeUpdateLoopStarted(this);
            }
        }

        public void Update()
        {
            try
            {
                using (_nodeUpdateTimer.NewContext())
                {
                    for (int i = 0; i < _config.MaxMessageToProcessPerTick; i++)
                    {
                        if (!Net.PumpEvents())
                            break;
                    }

                    OnNodeUpdate();
                }
            }
            catch (Exception ex)
            {
                _logger.NodeUpdateLoopError(this, ex);
            }
            
        }

        protected virtual void OnNodeUpdate() { }

        void UpdateLoop()
        {
            while (!_stopUpdateLoopSignal.IsSet)
            {
                Update();
                Thread.Sleep(1);
            }
        }

        void ProcessNewConnection(ITransportChannel transportChannel)
        {
            _netChannelsConnectedMeter.Mark();
            _netChannelsCounter.Increment();

            TNetChannel channel = ChannelFactory(this, transportChannel);

            if (!ChannelsById.TryAdd(channel.TransportChannel.LocalId, channel))
                throw new Exception("NetId should increment monotonically or never called from different threads");
            channel.DisconnectedEvent.Subscribe(OnPeerDisconnected);

            _logger.NetChannelConnected(this, channel);

            //complete future for whoever was waiting for this connection
            PendingConnection continuation;
            if (_pendingConnections.TryRemove(transportChannel.EndPoint, out continuation))
            {
                _logger.ConnectionSucceeded(this, continuation.Target, channel, (DateTime.Now - continuation.StartTime).Milliseconds);
                continuation.Expiration.Cancel();
                continuation.TCS.SetResult(channel);
            }

            _onNodeConnectedSubject.OnNext(channel);
        }

        void OnPeerDisconnected(INetChannel channel)
        {
            _netChannelsCounter.Decrement();
            _netChannelsDisconnectedMeter.Mark();

            TNetChannel savedPeer;
            if (ChannelsById.TryRemove(channel.TransportChannel.LocalId, out savedPeer))
            {
                if (!ReferenceEquals(savedPeer, channel))
                    throw new Exception("WTF! channel in dictionary is not the same as in disconnect event");

                _onNodeDisconnectedSubject.OnNext(savedPeer);
                _logger.NetChannelDisconnected(this, channel);
            }
        }

        public Task<INetChannel> Connect(IPEndPoint endPoint, CancellationToken cancellationToken)
        {
            //check if we are already connected to this endpoint
            foreach (var pair in ChannelsById)
            {
                if (endPoint.Equals(pair.Value.TransportChannel.EndPoint))
                    return Task.FromResult((INetChannel)pair.Value);
            }

            return _pendingConnections.AddOrUpdate(endPoint,
                    (key) =>
                    {
                        var val = new PendingConnection(key);
                        _logger.ConnectingTo(this, endPoint);
                        Net.Connect(endPoint);

                        var expiration = Task.Delay(TimeSpan.FromSeconds(_config.ConnectTimeoutSec), val.Expiration.Token);
                        expiration.ContinueWith( task =>
                            {
                                if (!task.IsCanceled)
                                {
                                    _logger.ConnectionHasTimedOut(this, endPoint);
                                    PendingConnection cont;
                                    if (_pendingConnections.TryRemove(key, out cont))
                                        cont.TCS.SetException(new ConnectionFailedException(key));
                                }
                            }, cancellationToken);
                        return val;
                    },
                    (key, val) => val
                ).TCS.Task;
        }

        public Task<INetChannel> Connect(IPEndPoint endPoint)
        {
            return Connect(endPoint, CancellationToken.None);
        }

        async public Task<INetChannel> Connect(string endpoint, CancellationToken cancellationToken)
        {
            return await Connect(await ResolveEndpoint(endpoint), cancellationToken);
        }

        async Task<IPEndPoint> ResolveEndpoint(string endpoint)
        {
            var uri = new Uri(endpoint);
            var ips = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
            return new IPEndPoint(ips.First(x => x.AddressFamily == AddressFamily.InterNetwork), uri.Port);
        }

        public INetChannel GetChannel(uint channelId)
        {
            TNetChannel peer;
            if (ChannelsById.TryGetValue(channelId, out peer))
                return peer;
            else
                return default(TNetChannel);
        }

        bool _isDisposed = false;
        void Dispose(bool isManual)
        {
            if(!_isDisposed)
            {
                if(isManual)
                {
                    _logger.NodeStopped(this);
                    Net.Dispose();
                }

                _stopUpdateLoopSignal.Set();
                _isDisposed = true;
            }
        }

        ~NetNode()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return $"'{InstanceName}'";
        }
    }
}
