using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive.Subjects;
using MOUSE.Core;
using MOUSE.Core.Actors;

#if !UNITY
using NLog;
#endif

namespace MOUSE.Unity
{
    public interface INode
    {
        IMessageFactory MsgFactory { get; }
        IOperationDispatcher Dispatcher { get; }

        void Start();
        void Stop();
    }

    public interface INetNode<TNetPeer> : INode
        where TNetPeer : INetPeer
    {
        Future<TNetPeer> ConnectToServer(IPEndPoint serverEndPoint);
        Future<TNetPeer> ConnectToServer(string serverEndPoint);

        IObservable<TNetPeer> PeerConnectedEvent { get; }
        IObservable<TNetPeer> PeerDisconnectedEvent { get; }

        IPEndPoint Endpoint { get; }

        TNetPeer GetPeer(uint localPeerId);
    }

    public interface INetPeer : INetChannelListener
    {
        void Init(INetChannel channel, INode node);

        INetChannel Channel { get; }

        IObservable<INetPeer> DisconnectedEvent { get; }
        IObservable<Message> MessageEvent { get; }

        IMessageFactory MsgFactory { get; }

        void Update();

        TNetContract GetProxy<TNetContract>(uint? localActorId = null) where TNetContract : class;
        void SetHandler<TNetContract>(TNetContract implementer) where TNetContract : class;

        void ExecuteOneWayOperation(Message inMsg);
        Future<Message> ExecuteOperation(Message inMsg, Action<Message> onResult, Action<ushort, string> onError);
    }


#if UNITY
    public class Logger
    {
        public void Debug(string message, params object[] objs)
        {
            //System.Diagnostics.Debug.WriteLine(string.Format(message, objs));
            //Console.WriteLine(string.Format(message, objs));
        }

        public void Info(string message, params object[] objs)
        {
            //System.Diagnostics.Debug.Print(string.Format(message, objs));
            //Console.WriteLine(string.Format(message, objs));
        }

        public void Error(string message, params object[] objs)
        {
            //System.Diagnostics.Debug.Print(string.Format(message, objs));
            //Console.WriteLine(string.Format(message, objs));
        }
    }

    /// <summary>
    /// Stub for lack of normal logging in Unity
    /// </summary>
    public static class LogManager
    {
        public static Logger GetLogger(string name)
        {
            return new Logger();
        }
    }
#endif

    public class NetPeer : INetPeer
    {
        Logger Log = LogManager.GetLogger("RPCPeer");
        private int _requestId = 0;
        private int _operationTimeoutSec = 30;

        private Dictionary<uint, object> _handlersByNetContractId;
        private Dictionary<ActorProxyKey, NetProxy> _proxyCache;
        private Dictionary<int, Future<Message>> _pendingOperation;


        public INetChannel Channel { get; private set; }
        public INode Node { get; private set; }

        public IMessageFactory MsgFactory { get { return Node.MsgFactory; } }
        public IOperationDispatcher Dispatcher { get { return Node.Dispatcher; } }


        private Subject<INetPeer> _onDisconnectedSubject;
        public IObservable<INetPeer> DisconnectedEvent { get { return _onDisconnectedSubject; } }

        private Subject<Message> _onMessageSubject;
        public IObservable<Message> MessageEvent { get { return _onMessageSubject; } }
        
        //precreated list to not create new object all the time in update
        private List<int> _expiredRequests = new List<int>();

        public void Init(INetChannel channel, INode node)
        {
            Channel = channel;
            Node = node;
            Log = LogManager.GetLogger(ToString());
            _onMessageSubject = new Subject<Message>();
            _onDisconnectedSubject = new Subject<INetPeer>();
            _pendingOperation = new Dictionary<int, Future<Message>>();
            _proxyCache = new Dictionary<ActorProxyKey, NetProxy>();
            _handlersByNetContractId = new Dictionary<uint, object>();
        }
        
        public void Update()
        {
            _expiredRequests.Clear();
            foreach (KeyValuePair<int, Future<Message>> pending in _pendingOperation)
            {
                if (DateTime.UtcNow > pending.Value.StartTime.AddSeconds(_operationTimeoutSec))
                {
                    pending.Value.SetError((ushort)BasicErrorCode.OperationTimeout, string.Format("Operation timed out after {0}s ", (DateTime.UtcNow - pending.Value.StartTime).TotalSeconds));
                    _expiredRequests.Add(pending.Key);
                }
            }

            foreach (int requestId in _expiredRequests)
            {
                _pendingOperation.Remove(requestId);
            }
        }

        public TNetContract GetProxy<TNetContract>(uint? localActorId = null) where TNetContract : class
        {
            NetContractDescription contractDesc = Dispatcher.GetContract<TNetContract>();
            var proxyKey = new ActorProxyKey(new ActorKey(0, localActorId ?? 0), contractDesc.TypeId);
            NetProxy proxy;
            if (!_proxyCache.TryGetValue(proxyKey, out proxy))
            {
                proxy = Dispatcher.CreateProxy(contractDesc.TypeId, MsgFactory, this, localActorId.HasValue ? proxyKey.ActorKey : (ActorKey?)null);
                _proxyCache.Add(proxyKey, proxy);
            }

            return (TNetContract)(object)proxy;
        }

        public void SetHandler<TNetContract>(TNetContract implementer) where TNetContract : class
        {
            uint serviceKey = Dispatcher.GetContractId(typeof(TNetContract));
            if (implementer == null)
            {
                _handlersByNetContractId.Remove(serviceKey);
            }
            else
            {
                _handlersByNetContractId[serviceKey] = implementer;
            }
        }

        public void OnNetData(BinaryReader reader)
        {
            Message msg = MsgFactory.Deserialize(reader);

            
            var opHeader = msg.GetHeader<OperationHeader>();
            if (opHeader != null && opHeader.Type == OperationType.Reply)
            {
                Future<Message> future;
                if (_pendingOperation.TryGetValue(opHeader.RequestId, out future))
                {
                    if(msg is InvalidOperation)
                    {
                        var inv = msg as InvalidOperation;
                        future.SetError(inv.ErrorCode, inv.DebugDescription);
                    }
                    else
                    {
                        future.SetResult(msg);
                    }
                    _pendingOperation.Remove(opHeader.RequestId);
                }
            }
            else 
            {
                NetContractDescription contractDesc = Dispatcher.GetContractForMessage(msg.Id);

                object handler;
                if (_handlersByNetContractId.TryGetValue(contractDesc.TypeId, out handler))
                {
                    Dispatcher.DispatchOneWay(handler, msg);
                }
            }

            MsgFactory.Free(msg);
        }

        public void OnDisconnected()
        {
            foreach (KeyValuePair<int, Future<Message>> pending in _pendingOperation)
            {
                pending.Value.SetError((ushort)BasicErrorCode.OperationTimeout, "Channel has closed");
            }
            _pendingOperation.Clear();
        }

        public void ExecuteOneWayOperation(Message inMsg)
        {
            Channel.Send(inMsg);
            MsgFactory.Free(inMsg);
        }

        public Future<Message> ExecuteOperation(Message inMsg, Action<Message> onResult, Action<ushort, string> onError)
        {
            _requestId++;
            inMsg.AttachHeader(new OperationHeader(_requestId, OperationType.Request));
            Channel.Send(inMsg);
            MsgFactory.Free(inMsg);
            var future = new Future<Message>(onResult, onError);
            _pendingOperation.Add(_requestId, future);
            return future;
        }
    }

    /// <summary>
    /// Not thread safe
    /// </summary>
    public class NetNode<TNetPeer> : INetNode<TNetPeer>, INetChannelConsumer
        where TNetPeer : INetPeer
    {
        private const int MaxMessagesPerTick = 100000;
        private const int ExpirationTimeout = 10;

        public Logger Log;

        protected readonly Dictionary<uint, TNetPeer> PeersByNetId = new Dictionary<uint, TNetPeer>();
        private readonly Dictionary<IPEndPoint, PendingConnection<TNetPeer>> _pendingConnections = new Dictionary<IPEndPoint, PendingConnection<TNetPeer>>();

        protected int RequestId = 0;

        public IMessageFactory MsgFactory { get; set; }
        public INetProvider Net { get; set; }
        public IOperationDispatcher Dispatcher { get; set; }

        protected Func<TNetPeer> PeerFactory;

        public NetNode(INetProvider net, IMessageFactory msgFactory, IOperationDispatcher protocol, Func<TNetPeer> peerFactory = null)
        {
            Net = net;
            MsgFactory = msgFactory;
            Dispatcher = protocol;

            if (peerFactory != null)
                PeerFactory = peerFactory;
            else
            {
                PeerFactory = () => (TNetPeer)Activator.CreateInstance(typeof(TNetPeer));
            }
        }

        public virtual void Start()
        {
            Log = LogManager.GetLogger(ToString());
            if (!Net.Init(this))
                throw new Exception("Net layer can't initialize");
        }

        public virtual void Stop()
        {
            Net.Shutdown();
        }


        private List<PendingConnection<TNetPeer>> _expiredConnections = new List<PendingConnection<TNetPeer>>();
        public virtual void Update()
        {
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                if (!Net.PumpEvents())
                    break;
            }

            _expiredConnections.Clear();
            foreach (var pendingConnection in _pendingConnections.Values)
            {
                if (pendingConnection.Future.StartTime.AddSeconds(ExpirationTimeout) < DateTime.UtcNow)
                    _expiredConnections.Add(pendingConnection);
            }

            foreach (var pendingConnection in _expiredConnections)
            {
                _pendingConnections.Remove(pendingConnection.Target);
                pendingConnection.Future.SetError((ushort)BasicErrorCode.ConnectionFailed,"Cant connect to -" + pendingConnection.Target);
            }

            OnNodeUpdate();
        }

        INetChannelListener INetChannelConsumer.OnNetConnect(INetChannel channel)
        {
            Log.Info("NetId:{0} has connected", channel.Id);
            TNetPeer peer = CreatePeer(channel);
            peer.DisconnectedEvent.Subscribe(OnPeerDisconnected);
            PeersByNetId.Add(peer.Channel.Id, peer);

            PendingConnection<TNetPeer> continuation;
            if(_pendingConnections.TryGetValue(channel.EndPoint, out continuation))
            {
                _pendingConnections.Remove(channel.EndPoint);
                continuation.Future.SetResult(peer);
            }
            _onNodeConnectedSubject.OnNext(peer);

            return peer;
        }

        void OnPeerDisconnected(INetPeer peer)
        {
            Log.Info("NetId:{0} has disconnected", peer.Channel.Id);
            TNetPeer savedPeer;
            if (PeersByNetId.TryGetValue(peer.Channel.Id, out savedPeer))
            {
                PeersByNetId.Remove(peer.Channel.Id);
                if (!object.ReferenceEquals(savedPeer, peer))
                    throw new Exception("WTF! peer in dictionary is not the same as in disconnect event");
                Log.Info("{0} has disconnected", savedPeer);
                _onNodeDisconnectedSubject.OnNext(savedPeer);
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

        public Future<TNetPeer> ConnectToServer(IPEndPoint endPoint)
        {
            //check if we are already connected to this endpoint
            foreach (var pair in PeersByNetId)
            {
                if (endPoint.Equals(pair.Value.Channel.EndPoint))
                {
                    var future = new Future<TNetPeer>();
                    future.SetResult(pair.Value);
                    return future;
                }
                    
            }
            PendingConnection<TNetPeer> pendingConnection;
            if (!_pendingConnections.TryGetValue(endPoint, out pendingConnection))
            {
                pendingConnection = new PendingConnection<TNetPeer>(endPoint);
                _pendingConnections.Add(endPoint, pendingConnection);
                Net.Connect(endPoint);
            }

            return pendingConnection.Future;
        }

        public Future<TNetPeer> ConnectToServer(string serverEndPoint)
        {
            return ConnectToServer(NodeRemoteInfo.ParseIPEndPoint(serverEndPoint));
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

    public class PendingConnection<TNetPeer>
        where TNetPeer : INetPeer
    {
        public readonly Future<TNetPeer> Future;
        public readonly IPEndPoint Target;

        public PendingConnection(IPEndPoint target)
        {
            Future = new Future<TNetPeer>();
            Target = target;
        }
    }
}
