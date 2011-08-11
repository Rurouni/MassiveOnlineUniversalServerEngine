using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization;

namespace MOUSE.Core
{
    public enum NodeMessageId: uint
    {
        Empty = 1,
        ConnectionRequest = 2,
        ConnectionReply = 3,
        UpdateClusterInfo = 4,
        InvalidEntityOperation = 5,
        EntityDiscoveryRequest = 6,
        EntityDiscoveryReply = 7,
        Last //used for protocol generation
    }


    [Export(typeof(Message))]
    [DataContract]
    public sealed class EmptyMessage : Message
    {
        public override uint Id { get { return (uint)NodeMessageId.Empty; } }
    }

    [Export(typeof(Message))]
    [DataContract]
    public sealed class ConnectRequest : Message
    {
        [DataMember]
        public NodeDescription Description;

        public override uint Id { get { return (uint)NodeMessageId.ConnectionRequest; } }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }

        public override void Deserialize(NativeReader reader)
        {
            base.Deserialize(reader);
            Description = new NodeDescription(reader);
        }
    }

    [Export(typeof(Message))]
    [DataContract]
    public sealed class ConnectReply : Message
    {
        [DataMember]
        public NodeDescription Description;

        public override uint Id { get { return (uint)NodeMessageId.ConnectionReply; } }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }

        public override void Deserialize(NativeReader reader)
        {
            base.Deserialize(reader);
            Description = new NodeDescription(reader);
        }
    }

    [Export(typeof(Message))]
    [DataContract]
    public sealed class UpdateClusterInfo : Message
    {
        [DataMember]
        public List<NodeDescription> Descriptions;

        public override uint Id { get { return (uint)NodeMessageId.UpdateClusterInfo; } }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Descriptions.Count);
            foreach (var description in Descriptions)
                description.Serialize(writer);
        }

        public override void Deserialize(NativeReader reader)
        {
            base.Deserialize(reader);

            int count = reader.ReadInt32();
            Descriptions = new List<NodeDescription>(count);
            for (int i = 0; i < count; i++)
                Descriptions.Add(new NodeDescription(reader));
        }
    }

    [Export(typeof(Message))]
    [DataContract]
    public sealed class EntityDiscoveryRequest : Message
    {
        public override uint Id { get { return (uint)NodeMessageId.EntityDiscoveryRequest; } }
    }

    [Export(typeof(Message))]
    [DataContract]
    public sealed class EntityDiscoveryReply : Message
    {
        [DataMember]
        public NodeDescription Description;

        public override uint Id { get { return (uint)NodeMessageId.EntityDiscoveryReply; } }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }

        public override void Deserialize(NativeReader reader)
        {
            base.Deserialize(reader);
            Description = new NodeDescription(reader);
        }
    }

    [Export(typeof(Message))]
    [DataContract]
    public sealed class InvalidEntityOperation : Message
    {
        public override uint Id { get { return (uint)NodeMessageId.InvalidEntityOperation; } }
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
