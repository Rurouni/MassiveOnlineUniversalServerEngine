using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

    public class OperationContext
    {
        public readonly Message Message;
        public readonly INetNode<INetPeer> Node;
        public readonly INetPeer Source;

        public OperationContext(INetNode<INetPeer> node, Message message, NetPeer source)
        {
            Node = node;
            Message = message;
            Source = source;
        }
    }
    
    public interface INetPeer : INetChannelListener
    {
        INetChannel Channel {get;}

        IObservable<INetPeer> DisconnectedEvent { get; }
        IObservable<Message> MessageEvent { get; }

        Task<Message> ExecuteOperation(Message input);
    }
    /// <summary>
    /// Different peers could simultaniously receive events, so implementation should be aware of this
    /// </summary>
    public interface INetNode<out TNetPeer> : IDisposable
        where TNetPeer : INetPeer
    {
        IMessageFactory MessageFactory { get; }
        
        /// <summary>
        /// should be called only for manually updated Nodes
        /// </summary>
        void Update();

        Task<INetPeer> Connect(IPEndPoint endPoint);//Why Task<T> is not covariant???

        IObservable<TNetPeer> PeerConnectedEvent { get; }
        IObservable<TNetPeer> PeerDisconnectedEvent { get; }
        
    }

    public interface IServiceNode
    {
        Task<Message> ExecuteServiceOperation(NodeServiceProxy proxy, Message input);
        Task<TNetContract> GetService<TNetContract>(uint serviceLocalId = 0);
        IServiceProtocol Protocol { get; }
        IMessageFactory MessageFactory { get; }
    }

    public class Fiber
    {
        readonly ActionBlock<Action> _processingQueue;
        private readonly bool _manualUpdate;
        readonly ConcurrentQueue<Action> _manualProcessingQueue = new ConcurrentQueue<Action>(); 

        public Fiber(TaskScheduler scheduler, bool manualUpdate = false)
        {
            _processingQueue = new ActionBlock<Action>((func) => func(),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = scheduler
                });
            _manualUpdate = manualUpdate;
        }

        public Fiber(bool manualUpdate = false)
            :this(TaskScheduler.Default, manualUpdate)
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
            Process(async()=>
                {
                    try
                    {
                        T result = await func();
                        tcs.SetResult(result);
                    }
                    catch(Exception ex)
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
            while (limit++ <10000 && _manualProcessingQueue.TryDequeue(out action))
                action();
        }
    }

    public class NetPeer : INetPeer
    {
        private const int ExpirationTimeout = 30;

        public Logger Log;
        public INetChannel Channel { get; private set; }
        private readonly Subject<INetPeer> _onDisconnectedSubject = new Subject<INetPeer>();
        private readonly Subject<Message> _onMessageSubject = new Subject<Message>();
        public INetNode<INetPeer> Owner {get; private set;}

        private int RequestId = 0;
        protected readonly ConcurrentDictionary<int, PendingOperation> PendingOperationsByRequestId
            = new ConcurrentDictionary<int, PendingOperation>();

        public NetPeer(INetChannel channel, INetNode<INetPeer> owner)
        {
            Channel = channel;
            Owner = owner;
            Log = LogManager.GetLogger(ToString());
        }

        void INetChannelListener.OnDisconnected()
        {
            _onDisconnectedSubject.OnNext(this);
            _onDisconnectedSubject.OnCompleted();
        }

        void INetChannelListener.OnNetData(NativeReader reader)
        {
            Message msg = Owner.MessageFactory.Deserialize(reader);
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
                        continuation.TCS.SetException(new InvalidOperationException(invMsg.ErrorCode, invMsg.DebugDescription));
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
            return MessageEvent.OfType<T>().StartAsync(cancelation.Token);
        }

        public Task<Message> ExecuteOperation(Message input)
        {
            int requestId = Interlocked.Increment(ref RequestId);
            input.AttachHeader(new OperationHeader(requestId, OperationType.Request));
            Channel.Send(input);

            var continuation = new PendingOperation(requestId);
            if (!PendingOperationsByRequestId.TryAdd(requestId, continuation))
                throw new Exception("This could happen only if requestId is duplicated");

            var expiration = TaskEx.Delay(TimeSpan.FromSeconds(ExpirationTimeout), continuation.Expiration.Token);
            expiration.ContinueWith((_) =>
            {
                PendingOperation dummy;
                if (!PendingOperationsByRequestId.TryRemove(continuation.RequestId, out dummy))
                    throw new Exception("Ids for requests should generate monotonicaly and never duplicate");
                continuation.TCS.SetException(new Exception(continuation + " has Expired in " + ExpirationTimeout));
            });
            expiration.Start();
            return continuation.TCS.Task;
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
    }

    public class NodeIssue
    {
        public NodeIssueType Type;
        public object Data;
    }

    public enum NodeIssueType
    {
        ConnectedEvent,
        DisconnectedEvent,
        ConnectCommand

    }

    /// <summary>
    /// is thread safe
    /// </summary>
    public class NetNode<TNetPeer> : INetNode<TNetPeer>, INetPeerFactory
        where TNetPeer : INetPeer
    {
        private const int MaxMessagesPerTick = 100000;
        private const int MaxEventsPerTick = 100000;
        private const int ExpirationTimeout = 30;

        public Logger Log;

        protected readonly ConcurrentDictionary<uint, TNetPeer> PeersByNetId
            = new ConcurrentDictionary<uint, TNetPeer>();
        private readonly ConcurrentDictionary<IPEndPoint, PendingConnection> _pendingConnections
            = new ConcurrentDictionary<IPEndPoint, PendingConnection>();
        
        
        private int _updateLoopRunning = 0;
        private Thread _updateThread;

        private Func<INetChannel, TNetPeer> _peerFactory;
                        
        protected int RequestId = 0;
        
        public IMessageFactory MessageFactory { get; set; }
        public INetProvider Net { get; set; }
        public IServiceProtocol Protocol { get; set; }

        public NetNode(INetProvider net, IMessageFactory msgFactory, IServiceProtocol protocol,
            Func<INetChannel, TNetPeer> peerFactory = null,
            bool manualUpdate = false)
        {
            Net = net;
            MessageFactory = msgFactory;
            Protocol = protocol;

            Log = LogManager.GetLogger(ToString());

            if (!manualUpdate)
            {
                _updateLoopRunning = 1;
                _updateThread = new Thread(UpdateLoop);
                _updateThread.Start();
                Log.Info("UpdateThread started");
            }
            Log.Info("Created on {0}", Net.EndPoint.ToString());
        }


        public virtual void Update()
        {
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                if (!Net.PumpEvents())
                    break;
            }

            OnNodeUpdate();
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

        INetChannelListener INetPeerFactory.OnNetConnect(INetChannel channel)
        {
            Log.Info("NetId:{0} has connected", channel.Id);
            TNetPeer peer = _peerFactory(channel);
            peer.DisconnectedEvent.Subscribe(OnPeerDisconnected);
            if(!PeersByNetId.TryAdd(peer.Channel.Id, peer))
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
        {}

        public virtual TNetPeer CreatePeer(INetChannel channel)
        {
            if (_peerFactory != null)
                return _peerFactory(channel);
            else throw new Exception("_peerFactory is not initialized neither overriden");
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
                    return TaskEx.FromResult((INetPeer)pair.Value);
            }

            return _pendingConnections.AddOrUpdate(endPoint,
                        (key) =>
                        {
                            var val = new PendingConnection(key);
                            Log.Info("Connecting to " + endPoint);
                            Net.Connect(endPoint);

                            var expiration = TaskEx.Delay(TimeSpan.FromSeconds(ExpirationTimeout),
                                                          val.Expiration.Token);
                            expiration.ContinueWith((_) =>
                                    {
                                        PendingConnection cont;
                                        if (_pendingConnections.TryRemove(key, out cont))
                                            cont.TCS.SetException(new Exception("Cant connect to -" + key));
                                    });
                            expiration.Start();
                            return val;
                        },
                        (key, val) => val
                 ).TCS.Task;
        }
        
        public override string ToString()
        {
            return "NetNode - "+ Net.EndPoint;
        }

        public void Dispose()
        {
            if(_updateLoopRunning == 1)
            {
                Interlocked.Exchange(ref _updateLoopRunning, 0);
                _updateThread.Join();
            }
        }
    }

    public class InvalidOperationException : Exception
    {
        public ushort ErrorCode;

        public InvalidOperationException(ushort errorCode, string debugMessage)
            :base(debugMessage)
        {
            ErrorCode = errorCode;
        }
    }
}
