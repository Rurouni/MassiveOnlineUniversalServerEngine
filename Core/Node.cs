using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NLog;
using RakNetWrapper;
using System.Threading.Tasks;

namespace MOUSE.Core
{
    [DataContract]
    public abstract class Message
    {
        [DataMember]
        private List<MessageHeader> _headers;

        public void AttachHeader(MessageHeader header)
        {
            if(_headers == null)
                _headers = new List<MessageHeader>();

            _headers.Add(header);
        }

        public THeader GetHeader<THeader>() where THeader: MessageHeader
        {
            if (_headers == null)
                return default(THeader);

            return (THeader)_headers.FirstOrDefault(x => x is THeader);
        }

        public virtual MessagePriority Priority
        {
            get { return MessagePriority.MEDIUM_PRIORITY; }
        }

        public virtual MessageReliability Reliability
        {
            get { return MessageReliability.RELIABLE_ORDERED; }
        }
    }

    public enum NodeType
    {
        Server,
        Client,
        Master
    }

    public class PendingConnection
    {
        public readonly TaskCompletionSource<NetId> TCS;
        public readonly DateTime StartTime;
        public readonly IPEndPoint Target;

        public PendingConnection(TaskCompletionSource<NetId> tcs, DateTime startTime, IPEndPoint target)
        {
            TCS = tcs;
            StartTime = startTime;
            Target = target;
        }
    }

    public class PendingOperation
    {
        public readonly object TCS; //uses object because only generic TaskCompletionSource<> version exists
        public readonly uint RequestId;
        public readonly DateTime StartTime;
    }

    /// <summary>
    /// is not thread safe
    /// </summary>
    public class Node
    {
        private const int MaxMessagesPerTick = 100000;

        public Logger Log = LogManager.GetCurrentClassLogger();

        public readonly NodeType NType;
        private NodeDescription _selfDescription;
        private readonly IPEndPoint _masterNodeEndPoint;
        private readonly IPEndPoint _selfEndpoint;
        private readonly NativeReader _reader = new NativeReader();
        private readonly NativeWriter _writer = new NativeWriter();
        private readonly RakPeerInterface _netPeer;
        private readonly FastSerializer _serializer = new FastSerializer();
        private readonly Dictionary<NetId, NodeDescription> _connectedNodes;
        private long _updateLoopRunning = 0;
        private AutoResetEvent _updateLoopFinishedEvent;
        private ProxyFactory _proxyFactory = new ProxyFactory();
        private NodeEntityRepository _entityRepository = new NodeEntityRepository();
        private Dictionary<IPEndPoint, TaskCompletionSource<NetId>> _pendingConnections = new Dictionary<IPEndPoint, TaskCompletionSource<NetId>>();
        
        //<requestId,>
        private Dictionary<uint, TaskCompletionSource<NetId>> _pendingOperations = new Dictionary<IPEndPoint, TaskCompletionSource<NetId>>();
        private uint _requestId = 0;

        public Node(NodeType type, IPEndPoint selfEndpoint, IPEndPoint masterNodeEndPoint = null)
        {
            _masterNodeEndPoint = masterNodeEndPoint;
            _selfEndpoint = selfEndpoint;
            if(type == NodeType.Client && _masterNodeEndPoint == null)
                throw new Exception("Client node must have valid masterNodeEndpoint");
            NType = type;

            _netPeer = new RakPeerInterface();
            _reader.SetBuffer(new byte[1024*1024*10], 0);
        }

        public void Start(bool manualUpdate)
        {
            var res = _netPeer.Startup(_selfEndpoint, 1000, 10000);
            if (res != StartupResult.RAKNET_STARTED)
                throw new Exception("NetLayer failed to start:" + res);

            var endpoint = _netPeer.GetActiveEndPoint();
            _selfDescription = new NodeDescription
            {
                NodeId = _netPeer.Id().Id,
                Ip = endpoint.Address.ToString(),
                Port = (ushort)endpoint.Port,
                Type = NType
            };


            if (!manualUpdate)
            {
                _updateLoopFinishedEvent = new AutoResetEvent(false);
                _updateLoopRunning = 1;
                var thread = new Thread(UpdateLoop);
                thread.Start();
            }
        }

        public void Stop()
        {
            if (_updateLoopRunning == 1)
            {
                Interlocked.Exchange(ref _updateLoopRunning, 0);
                _updateLoopFinishedEvent.WaitOne();
            }
            _netPeer.Shutdown();

        }

        public void Update()
        {
            var netId = new NetId();
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                int length = 0;
                if (_netPeer.Receive(ref netId, _reader.GetBuffer(), ref length))
                {
                    _reader.SetPosition(0);
                    _reader.SetLength(length);
                    ProcessNetEvent(netId, _reader);    
                }
            }
        }

        private void UpdateLoop()
        {
            Log.Info("UpdateLoop started");
            while (Interlocked.Read(ref _updateLoopRunning) == 1)
            {
                Update();
                Thread.Sleep(1);
            }
            Log.Info("UpdateLoop stopped");
            _updateLoopFinishedEvent.Set();

        }

        private void ProcessNetEvent(NetId netId, NativeReader reader)
        {
            var netMsgId = (RakNetMessages)reader.ReadByte();
            switch (netMsgId)
            {
                case RakNetMessages.ID_USER_PACKET_ENUM:
                    OnNetMessage(netId, reader);
                    break;
                case RakNetMessages.ID_DETECT_LOST_CONNECTIONS:
                case RakNetMessages.ID_CONNECTION_LOST:
                case RakNetMessages.ID_DISCONNECTION_NOTIFICATION:
                    OnNetDisconnect(netId);
                    break;
                case RakNetMessages.ID_NEW_INCOMING_CONNECTION:
                    OnNetConnected(netId);
                    Log.Debug("New incoming connection from Node<Id:{1}>", netId.Id);
                    break;
                case RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED:
                    OnNetConnectionAccepted(netId);
                    break;
                default:
                    Log.Warn("Unhandled MessageType:{0} from Node<Id:{1}>", netMsgId, netId.Id);
                    break;
            }
        }

        private void OnNetConnected(NetId senderId)
        {
            Log.Info("NetId<{0}> has connected", senderId.Id);
        }

        private void OnNetConnectionAccepted(NetId senderId)
        {
            Log.Info("Connection to NetId<{0}>  has been accepted", senderId.Id);

            Send(senderId, new NodeIntroductionRequest { SenderDescription = _selfDescription });
        }

        private void OnNetDisconnect(NetId senderId)
        {
            Log.Info("NetId<{0}> has disconnected", senderId.Id);
            NodeDescription desc;
            if (_connectedNodes.TryGetValue(senderId, out desc))
            {
                Log.Info("{0} has disconnected", desc);
                _connectedNodes.Remove(senderId);
                OnNodeDisconnected(senderId, desc);
            }
        }

        private void OnNetMessage(NetId senderId, NativeReader reader)
        {
            Log.Info("Message from NetId<{0}>", senderId.Id);
            object msg = _serializer.Deserialize(reader);

            if (msg is NodeIntroductionReply)
                OnNodeIntroductionReply(senderId, (NodeIntroductionReply)msg);
            else if (msg is NodeIntroductionRequest)
                OnNodeIntroductionRequest(senderId, (NodeIntroductionRequest)msg);

            //TODO: general processing logic
            
        }

        private void OnNodeIntroductionRequest(NetId senderId, NodeIntroductionRequest msg)
        {
            Log.Info("OnNodeIntroductionRequest<NetId:{0}, {1}>", senderId.Id, msg.SenderDescription);
            _connectedNodes.Add(senderId, msg.SenderDescription);
            OnNodeConnected(senderId, msg.SenderDescription);

            TaskCompletionSource<NetId> tcs;
            IPEndPoint endpoint = _netPeer.GetEndPoint(senderId);
            if (_pendingConnections.TryGetValue(endpoint))
            {
                tcs.SetResult(senderId);
                _pendingConnections.Remove(endpoint);
            }
            Send(senderId, new NodeIntroductionReply {ReceiverDescription = _selfDescription});
        }

        private void OnNodeIntroductionReply(NetId senderId, NodeIntroductionReply msg)
        {
            Log.Info("OnNodeIntroductionReply<NetId:{0}, {1}>", senderId.Id, msg.ReceiverDescription);
            _connectedNodes.Add(senderId, msg.ReceiverDescription);
            OnNodeConnected(senderId, msg.SenderDescription);

            TaskCompletionSource<NetId> tcs;
            IPEndPoint endpoint = _netPeer.GetEndPoint(senderId);
            if (_pendingConnections.TryGetValue(endpoint))
            {
                tcs.SetResult(senderId);
                _pendingConnections.Remove(endpoint);
            }
            else
                throw new Exception("Received OnNodeIntroductionReply without pending connection");
        }

        public virtual void OnNodeConnected(NetId netId, NodeDescription description)
        {}

        public virtual void OnNodeDisconnected(NetId netId, NodeDescription description)
        {}

        public Task<NetId> ConnectAsync(IPEndPoint endPoint)
        {
            TaskCompletionSource<NetId> tcs;

            //check if we are already connected to this endpoint
            foreach (var netId in _connectedNodes.Keys)
            {
                if (netId.EndPoint == _netPeer.GetEndPoint(netId))
                {
                    tcs = new TaskCompletionSource<NetId>();
                    tcs.SetResult(netId);
                    return tcs.Task;
                }
            }

            if (!_pendingConnections.TryGetValue(endPoint, out tcs))
            {
                tcs = new TaskCompletionSource<NetId>();
                _pendingConnections.Add(endPoint, tcs);
                _netPeer.Connect(endPoint);
            }
            return tcs.Task;

        }

        public void Send(NetId netId, Message msg)
        {
            _writer.Position = 0;
            _writer.Write((byte)RakNetMessages.ID_USER_PACKET_ENUM);
            _serializer.Serialize(msg, _writer);

            _netPeer.Send(netId, _writer.Buff, (int)_writer.Position, msg.Priority, msg.Reliability, 0, false);
        }

        public void SendLoopback(Message msg)
        {
            _writer.Position = 0;
            _writer.Write((byte)RakNetMessages.ID_USER_PACKET_ENUM);
            _serializer.Serialize(msg, _writer);

            _netPeer.SendLoopback(netId, _writer.Buff, (int)_writer.Position);
        }

        public TEntityContract GetProxy<TEntityContract>(ulong entityId) where TEntityContract : class
        {
            return (TEntityContract)(object)_proxyFactory.GetProxy<TEntityContract>(entityId, this);
        }

        public async Task<object> EntityOperationCallAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
            where TRequestMessage : Message
        {
            if (_entityRepository.Contains(caller.TargetEntityId))
            {
                SendLoopback(input);
            }
            
            NetId masterNetId = await ConnectToMaster();
            
        }
        
        //public abstract async Task EntityCallAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;

        //public abstract async void EntityCallOnewayAsync<TRequestMessage>(TRequestMessage input, NodeEntityProxy caller)
        //    where TRequestMessage : Message;

        public bool IsMasterConnected
        {
            get
            {
                if (_masterNodeEndPoint == null) //we are master
                    return true;
                return _connectedNodes.Values.Where(x => x.Type == NodeType.Master).Count() > 0;
            }
        }
    }

//    public class ServerNode : Node
//    {
//        public ServerNode(IPEndPoint selfEndpoint, IPEndPoint masterNodeEndPoint)
//            : base(NodeType.Server, selfEndpoint, masterNodeEndPoint)
//        {
//        }
//    }


//    public class ClientNode : Node
//    {
//        public ClientNode(IPEndPoint selfEndpoint, IPEndPoint masterNodeEndPoint)
//            : base(NodeType.Client, selfEndpoint, masterNodeEndPoint)
//        {
//        }

//        public override async Task<TReplyMessage> EntityCallAsync<TRequestMessage, TReplyMessage>(TRequestMessage input, NodeEntityProxy caller)
//        {
//            throw new NotImplementedException();
//        }
//    }
}
