using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization;
using RakNetWrapper;

namespace MOUSE.Core
{
    public enum NodeMessageId: uint
    {
        NodeIntroductionRequest = 1,
        NodeIntroductionReply = 2,
        Last //used for protocol generation
    }

    public class NodeIntroductionRequest : Message
    {
        [DataMember]
        public NodeDescription Description;

        public NodeIntroductionRequest(NodeDescription senderDescription) : base((uint)NodeMessageId.NodeIntroductionRequest)
        {
            Description = senderDescription;
        }

        public NodeIntroductionRequest(NativeReader reader) : base((uint)NodeMessageId.NodeIntroductionRequest, reader)
        {
            Description = new NodeDescription(reader);
        }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }
    }

    public class NodeIntroductionReply : Message
    {
        [DataMember]
        public NodeDescription Description;

        public NodeIntroductionReply(NodeDescription receiverDescription) : base((uint)NodeMessageId.NodeIntroductionReply)
        {
            Description = receiverDescription;
        }

        public NodeIntroductionReply(NativeReader reader) : base((uint)NodeMessageId.NodeIntroductionReply, reader)
        {
            Description = new NodeDescription(reader);
        }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }
    }

    [DataContract]
    public class NodeDescription
    {
        [DataMember]
        public readonly ulong NodeId;
        [DataMember]
        public readonly NodeType Type;
        [DataMember]
        public readonly string Ip;
        [DataMember]
        public readonly ushort Port;

        private IPEndPoint _endPoint;
        public IPEndPoint EndPoint
        {
            get
            {
                if(_endPoint == null)
                    _endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
                return _endPoint;
            }
        }

        public NodeDescription(ulong nodeId, NodeType type, IPEndPoint endpoint)
        {
            NodeId = nodeId;
            Type = type;
            _endPoint = endpoint;
            Ip = endpoint.Address.ToString();
            Port = (ushort)endpoint.Port;
        }

        public NodeDescription(NativeReader reader)
        {
            NodeId = reader.ReadUInt64();
            Type = (NodeType)reader.ReadByte();
            Ip = reader.ReadASCII();
            Port = reader.ReadUInt16();
        }

        public void Serialize(NativeWriter writer)
        {
            writer.Write(NodeId);
            writer.Write((byte)Type);
            writer.WriteASCII(Ip);
            writer.Write(Port);
        }


        public override string ToString()
        {
            return string.Format("Node<Id:{0}, Type:{1}, Ip:{2}, Port:{3}>", NodeId, Type, Ip, Port);
        }
    }
}
