using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;
using RakNetWrapper;

namespace MOUSE.Core
{
    public enum NodeType
    {
        Server,
        Client
    }

    public class Message
    {
        public TransportHeader TransportHeader;
        public IssueHeader IssueHeader;
        public object Body;
    }

    public class NodeIntroductionRequest
    {
        public NodeDescription NewNode;
    }

    public class NodeIntroductionReply
    {
        public List<NodeDescription> Cluster;
    }

    public class NodeDescription
    {
        public ulong NodeId;
        public NodeType Type;
        public string Ip;
        public ushort Port;

        public override string ToString()
        {
            return string.Format("Node<Id:{0}, Type:{1}, Ip:{2}, Port:{3}>", NodeId, Type, Ip, Port);
        }
    }

    
    /// <summary>
    /// is not thread safe
    /// </summary>
    public class Node
    {
        private const int MaxMessagesPerTick = 100000;

        public Logger Log = LogManager.GetCurrentClassLogger();

        private readonly NodeDescription _selfDescription;
        private readonly NativeReader _reader = new NativeReader();
        private readonly NativeWriter _writer = new NativeWriter();
        private readonly RakPeerInterface _netPeer;
        private readonly FastSerializer _serializer = new FastSerializer();
        private readonly Dictionary<NetId, NodeDescription> _connectedNodes;
        private long _updateLoopRunning = 0;
        private AutoResetEvent _updateLoopFinishedEvent;
        

        public Node(NodeDescription selfDescription)
        {
            _selfDescription = selfDescription;
            
            _netPeer = new RakPeerInterface();
            _reader.SetBuffer(new byte[1024*1024*10], 0);
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
                    Process(netId, _reader);    
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

        private void Process(NetId netId, NativeReader reader)
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
                    Log.Debug("New incoming connection from Node<Id:{1}>", packet.SenderNetId.Id);
                    break;
                case RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED:
                    SendIntroduction(netId);
                    break;
                default:
                    Log.Warn("Unhandled MessageType:{0} from Node<Id:{1}>", netMsgId, netId.Id);
                    break;
            }
        }

        private void SendIntroduction(NetId netId)
        {
            var msg = new Message
            {
                TransportHeader = new TransportHeader(TransportHeaderType.Direct),
                IssueHeader = new IssueHeader(IssueHeaderType.NodeMessage),
                Body = new NodeIntroductionRequest{NewNode = _selfDescription}
            };
            Send(netId, msg);
        }

        public void Send(NetId netId, Message msg, MessagePriority priority, MessageReliability reliability)
        {
            _writer.Position = 0;
            _writer.Write((byte)RakNetMessages.ID_USER_PACKET_ENUM);
            _serializer.Serialize(msg.TransportHeader, _writer);
            _serializer.Serialize(msg.IssueHeader, _writer);
            _serializer.Serialize(msg.Body, _writer);

            _netPeer.Send(netId, _writer.Buff, (int)_writer.Position, msg.Priority, msg.Reliability, 0, false);
        }

        private void OnNetConnect(NetId senderId)
        {
            Log.Info("NetId<{0}> has connected", senderId.Id);
        }

        private void OnNetDisconnect(NetId senderId)
        {
            Log.Info("NetId<{0}> has disconnected", senderId.Id);
            NodeDescription desc;
            if (_connectedNodes.TryGetValue(senderId, out desc))
            {
                Log.Info("{0} has disconnected", desc);
                _connectedNodes.Remove(senderId);
            }
        }

        private void OnNetMessage(NetId senderId, NativeReader reader)
        {
            Log.Info("Message from NetId<{0}>", senderId.Id);
            var transportHeader = (TransportHeader)_serializer.Deserialize(reader);
            var issueHeader = (IssueHeader)_serializer.Deserialize(reader);
            object body = _serializer.Deserialize(reader);

            if (issueHeader.Id == IssueHeaderType.NodeMessage)
                ProcessNodeMessage(body);
            else
                ProcessIssue(issueHeader, body)
            
        }

        private void ProcessNodeMessage(object body)
        {
            if (body is NodeIntroductionReply)
                OnNodeIntroductionReply((NodeIntroductionReply)body);
            else if (body is NodeIntroductionRequest)
                OnNodeIntroductionRequest((NodeIntroductionRequest)body);
                
        }

        private void OnNodeIntroductionRequest(NodeIntroductionRequest msg)
        {
            throw new NotImplementedException();
        }

        private void OnNodeIntroductionReply(NodeIntroductionReply msg)
        {
            throw new NotImplementedException();
        }

        public void Start(bool manualUpdate)
        {
            var res = _netPeer.Startup(_selfDescription.Ip, _selfDescription.Port, 1000, 10000);
            if (res != StartupResult.RAKNET_STARTED)
                throw new Exception("NetLayer failed to start:" + res);

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

        public void ConnectTo(string ip, ushort port)
        {
            _netPeer.Connect(ip, port);
        }
    }
}
