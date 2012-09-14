using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using ExitGames.Client.Photon;
using MOUSE.Core;
using System.Reactive.Linq;
using System.IO;

namespace UnityClient
{
    public interface INetClient
    {
        IEnumerable Connect(string address);
        IEnumerable Disconnect();
        void Update();

        void ExecuteOperation<TInMessage>(TInMessage inMsg) where TInMessage : Message;
        IEnumerable ExecuteOperation<TInMessage, TOutMessage>(TInMessage inMsg, OperationReply<TOutMessage> reply)
            where TInMessage : Message
            where TOutMessage : Message;

        TNetContract GetService<TNetContract>(uint serviceId = 0);
        void SetHandler<TNetContract>(TNetContract implementer);
        
        IObservable<INetClient> OnConnected { get; }
        IObservable<INetClient> OnDisconnected { get; }
        IObservable<Message> OnMessage { get; }
    }

    public class Logger
    {
        public void Debug(string message, params object[] objs)
        {
            System.Diagnostics.Debug.WriteLine(string.Format(message, objs));
            Console.WriteLine(string.Format(message, objs));
        }

        public void Info(string message, params object[] objs)
        {
            System.Diagnostics.Debug.Print(string.Format(message, objs));
            Console.WriteLine(string.Format(message, objs));
        }

        public void Error(string message, params object[] objs)
        {
            System.Diagnostics.Debug.Print(string.Format(message, objs));
            Console.WriteLine(string.Format(message, objs));
        }
    }

    public class PhotonClient : INetClient, IPhotonPeerListener
    {
        //readonly Logger Log = LogManager.GetCurrentClassLogger();
        readonly Logger Log = new Logger();
        private readonly PhotonPeer _peer;
        private readonly ConnectionProtocol _connProtocol;
        private readonly string _applicationName;
        readonly Subject<INetClient> _disconnectedSubject = new Subject<INetClient>();
        readonly Subject<INetClient> _connectedSubject = new Subject<INetClient>();
        readonly Subject<Message> _messageSubject = new Subject<Message>();
        private int _requestId = 0;
        private int _operationTimeoutSec = 10;

        private readonly IMessageFactory _factory;
        private readonly IServiceProtocol _protocol;

        private readonly Dictionary<uint, object> _handlersByNetContractId = new Dictionary<uint, object>();
        private readonly Dictionary<NodeServiceKey, NodeServiceProxy> _proxyCache = new Dictionary<NodeServiceKey, NodeServiceProxy>();

        public IMessageFactory Factory
        {
            get { return _factory; }
        }

        public IServiceProtocol Protocol
        {
            get { return _protocol; }
        }

        public PhotonClient(IMessageFactory factory,IServiceProtocol serviceProtocol, ConnectionProtocol connProtocol, string applicationName)
        {
            _factory = factory;
            _protocol = serviceProtocol;
            _connProtocol = connProtocol;
            _applicationName = applicationName;
            _peer = new PhotonPeer(this, connProtocol);
        }

        public IEnumerable Connect(string address)
        {
            if(!_peer.Connect(address, _applicationName))
                throw new ArgumentException("Incorrect address-" + address);

            IEnumerable<List<object>> chunks =
                OnConnected
                .FirstAsync()
                .Timeout(TimeSpan.FromSeconds(_operationTimeoutSec))
                .Collect(() => new List<object>(),
                        (list, msg) => list, list => list);

            return chunks;
        }

        public void Update()
        {
            _peer.Service();
        }

        public void ExecuteOperation<TInMessage>(TInMessage inMsg)
            where TInMessage : Message
        {
            int requestId = _requestId++;
            inMsg.AttachHeader(new OperationHeader(requestId, OperationType.Request));
            SendMessage(inMsg);
        }

        private void SendMessage(Message msg)
        {
            var data = new Dictionary<byte, object>();
            data[0] = msg.GetSerialized();
            bool isReliable = msg.Reliability == MessageReliability.Reliable || msg.Reliability == MessageReliability.ReliableOrdered;
            _peer.OpCustom(42, data, isReliable);
        }

        /// <summary>
        /// This function is intended to be used only with Unity coroutines, eventually it would produce Reply or throw TimeoutException
        /// </summary>
        public IEnumerable ExecuteOperation<TInMessage, TOutMessage>(TInMessage inMsg, OperationReply<TOutMessage> reply)
            where TInMessage :Message
            where TOutMessage :Message
        {
            int requestId = _requestId++;
            inMsg.AttachHeader(new OperationHeader(requestId, OperationType.Request));
            SendMessage(inMsg);

            IEnumerable<List<Message>> chunks =
                OnMessage.FirstAsync(msg =>
                {
                    var header = msg.GetHeader<OperationHeader>();
                    return header != null && header.Type == OperationType.Reply
                        && header.RequestId == requestId;
                })
                .Timeout(TimeSpan.FromSeconds(_operationTimeoutSec))
                .Catch((Exception ex) =>
                    Observable.Return((Message)new InvalidOperation(0, "Timed out after" + _operationTimeoutSec+"RequestMessage:" +typeof(TInMessage))))
                .Collect(() => new List<Message>(),
                        (list, msg) =>
                        {
                            list.Add(msg);
                            return list;
                        }, list => list);
            
            foreach (List<Message> messages in chunks)
            {
                if (messages.Count > 0)
                {
                    Message msg = messages.First();
                    if (msg is InvalidOperation)
                        reply.SetReply(null, msg as InvalidOperation);
                    else
                        reply.SetReply(msg as TOutMessage, null);
                }
                yield return null;
            }
        }

        void IPhotonPeerListener.DebugReturn(DebugLevel level, string message)
        {
            Log.Debug(message);
        }

        void IPhotonPeerListener.OnEvent(EventData eventData)
        {
            //TODO: pool it later
            BinaryReader reader = new BinaryReader(new MemoryStream((byte[])eventData.Parameters[0]));

            Message msg = _factory.Deserialize(reader);
            _messageSubject.OnNext(msg);
        }

        void IPhotonPeerListener.OnOperationResponse(OperationResponse operationResponse)
        {
            //TODO: pool it later
            BinaryReader reader = new BinaryReader(new MemoryStream((byte[])operationResponse.Parameters[0]));

            Message msg = _factory.Deserialize(reader);
            _messageSubject.OnNext(msg);
        }

        void IPhotonPeerListener.OnStatusChanged(StatusCode statusCode)
        {
            Log.Info(statusCode.ToString());
            switch (statusCode)
            {
                case StatusCode.Connect:
                    _connectedSubject.OnNext(this);
                    break;
                case StatusCode.TimeoutDisconnect:
                case StatusCode.Disconnect:
                    _disconnectedSubject.OnNext(this);
                    break;
            }
        }

        public IEnumerable Disconnect()
        {
            _peer.Disconnect();

            IEnumerable<List<object>> chunks =
                OnDisconnected
                .FirstAsync()
                .Collect(() => new List<object>(),
                        (list, msg) => list, list => list);

            return chunks;
        }

        public IObservable<INetClient> OnConnected
        {
            get { return _connectedSubject; }
        }

        public IObservable<INetClient> OnDisconnected
        {
            get { return _disconnectedSubject; }
        }

        public IObservable<Message> OnMessage
        {
            get { return _messageSubject; }
        }

        public TNetContract GetService<TNetContract>(uint serviceLocalId = 0)
        {

            NodeServiceKey serviceKey = Protocol.GetKey<TNetContract>(serviceLocalId);
            NodeServiceProxy proxy;
            if (!_proxyCache.TryGetValue(serviceKey, out proxy))
            {
                proxy = Protocol.CreateProxy(serviceKey, Factory, this);
               _proxyCache.Add(serviceKey, proxy);
            }

            return (TNetContract)(object)proxy;
        }

        public void SetHandler<TNetContract>(TNetContract implementer)
        {
            _handlersByNetContractId[Protocol.GetContractId(typeof(TNetContract))] = implementer;
        }
    }

    public class InvalidInput : Exception
    {
        public ushort ErrorCode;

        public InvalidInput(Enum errorCode, string debugMessage)
            : base(debugMessage)
        {
            ErrorCode = Convert.ToUInt16(errorCode);
        }

        public InvalidInput(ushort errorCode, string debugMessage)
            : base(debugMessage)
        {
            ErrorCode = errorCode;
        }

        public InvalidInput(Enum errorCode)
            : base("InvalidInput:" + errorCode)
        {
            ErrorCode = Convert.ToUInt16(errorCode);
        }
    }

    public class OperationReply<TMessage>
    {
        private TMessage _reply;
        public TMessage Reply
        {
            get
            {
                if (Error != null)
                    throw new InvalidInput(Error.ErrorCode, Error.DebugDescription);
                return _reply;
            }
        }

        public void SetReply(TMessage msg, InvalidOperation error)
        {
            _reply = msg;
            Error = error;
        }

        public InvalidOperation Error { get; set; }

        public bool IsValid
        {
            get { return Error == null; }
        }
    }

}
