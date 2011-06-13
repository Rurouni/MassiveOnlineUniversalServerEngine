using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;
using RakNetWrapper;

namespace Core
{
    public enum NodeType
    {
        Server,
        Client
    }

    public interface IProtocol
    {
        
    }

    public class Issue
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
    }

    
    /// <summary>
    /// is not thread safe
    /// </summary>
    public class Node
    {
        private const int MaxMessagesPerTick = 100000;

        public Logger Log = LogManager.GetCurrentClassLogger();

        private readonly NodeDescription _selfDescription;
        private readonly Domain _domain;
        private readonly IProtocol _protocol = new InternalProtocol();
        private readonly RakPeerInterface _netPeer;
        private readonly Dictionary<NetId, NodeDescription> _connectedNodes;
        private long _updateLoopRunning = 0;
        private AutoResetEvent _updateLoopFinishedEvent;
        private readonly Issue _nodeMessageIssue;
        

        public Node(NodeDescription selfDescription, Domain domain)
        {
            _selfDescription = selfDescription;
            _domain = domain;
            _nodeMessageIssue = new Issue
            {
                TransportHeader = new TransportHeader(TransportHeaderType.Direct),
                IssueHeader = new IssueHeader(IssueHeaderType.NodeMessage)
            };
            _netPeer = new RakPeerInterface();
        }
        
        
        public void Update()
        {
            for (int i = 0; i < MaxMessagesPerTick; i++)
            {
                InPacket packet = _netPeer.Receive();
                if(packet == null)
                    return;

                Process(packet);
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

        private void Process(InPacket packet)
        {
            var netMsgId = (RakNetMessages)packet.ReadUByte();
            switch (netMsgId)
            {
                case RakNetMessages.ID_USER_PACKET_ENUM:
                    OnNetMessage(packet);
                    break;
                case RakNetMessages.ID_DETECT_LOST_CONNECTIONS:
                case RakNetMessages.ID_CONNECTION_LOST:
                case RakNetMessages.ID_DISCONNECTION_NOTIFICATION:
                    OnNetDisconnect(packet.SenderNetId);
                    break;
                case RakNetMessages.ID_NEW_INCOMING_CONNECTION:
                    Log.Debug("New incoming connection from Node<Id:{1}>", packet.SenderNetId.Id);
                    break;
                case RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED:
                    SendIntroduction(packet.SenderNetId);
                    break;
                default:
                    Log.Warn("Unhandled MessageType:{0} from Node<Id:{1}>", netMsgId, packet.SenderNetId.Id);
                    break;
            }
        }

        private void SendIntroduction(NetId netId)
        {
            _nodeMessageIssue.Body = new NodeIntroductionRequest{NewNode = _selfDescription};
            Send(netId, _nodeMessageIssue);
        }

        private void Send(NetId netId, Issue issue)
        {
            var packet = new OutPacket();
            packet.WriteUByte((byte)RakNetMessages.ID_USER_PACKET_ENUM);
            issue.TransportHeader.Serialize(packet);
            issue.IssueHeader.Serialize(packet);
            if (issue.IssueHeader.Id == IssueHeaderType.NodeMessage)
                _nodeDomain.Serialize(issue.Body);
            else
                _domain.Serialize(issue.Body);

            _netPeer.Send(netId, packet);
        }

        private void OnNetConnect(NetId senderId)
        {
            throw new NotImplementedException();
        }

        private void OnNetDisconnect(NetId senderId)
        {
            throw new NotImplementedException();
        }

        private void OnNetMessage(InPacket msg)
        {
            
        }

        public void Start(bool manualUpdate)
        {
            var res = _netPeer.Startup(selfIp, selfPort, 1000, 10000);
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
