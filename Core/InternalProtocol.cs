using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization;

namespace MOUSE.Core
{
    public enum NodeMessageId : uint
    {
        Empty = 1,
        ConnectionRequest = 2,
        ConnectionReply = 3,
        UpdateClusterInfo = 4,
        InvalidOperation = 5,
        EntityDiscoveryRequest = 6,
        EntityDiscoveryReply = 7,
        ServiceAccessRequest = 8,
        ServiceAccessReply = 9,
        ConnectToService = 10,
        Last, //used for protocol generation
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
    public sealed class ServiceAccessRequest : Message
    {
        [DataMember]
        public ulong ServiceId;

        public override uint Id { get { return (uint)NodeMessageId.ServiceAccessRequest; } }

        public ServiceAccessRequest(ulong serviceId)
        {
            ServiceId = serviceId;
        }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ServiceId);
        }

        public override void Deserialize(NativeReader reader)
        {
            base.Deserialize(reader);
            ServiceId = reader.ReadUInt64();
        }
    }

    

    [Export(typeof(Message))]
    [DataContract]
    public sealed class ServiceAccessReply : Message
    {
        [DataMember]
        public bool IsValid;
        [DataMember]
        public NodeDescription ServiceOwner;

        public override uint Id { get { return (uint)NodeMessageId.ServiceAccessReply; } }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            writer.Write(IsValid);
            ServiceOwner.Serialize(writer);
        }

        public override void Deserialize(NativeReader reader)
        {
            base.Deserialize(reader);
            IsValid = reader.ReadBoolean();
            ServiceOwner = new NodeDescription(reader);
        }

        public ServiceAccessReply(bool isValid, NodeDescription serviceOwner)
        {
            IsValid = isValid;
            ServiceOwner = serviceOwner;
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
    public sealed class InvalidOperation : Message
    {
        [DataMember]
        public ushort ErrorCode;
        [DataMember]
        public string DebugDescription;

        public override uint Id { get { return (uint)NodeMessageId.InvalidOperation; } }

        public InvalidOperation(ushort errorCode, string debugDescription)
        {
            ErrorCode = errorCode;
            DebugDescription = debugDescription;
        }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ErrorCode);
            writer.WriteUnicode(DebugDescription);
        }

        public override void Deserialize(NativeReader reader)
        {
            base.Deserialize(reader);
            ErrorCode = reader.ReadUInt16();
            DebugDescription = reader.ReadUnicode();
        }
    }

    [DataContract]
    public class NodeDescription
    {
        [DataMember]
        public readonly ulong NodeId;
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

        public NodeDescription(ulong nodeId, IPEndPoint endpoint)
        {
            NodeId = nodeId;
            _endPoint = endpoint;
            Ip = endpoint.Address.ToString();
            Port = (ushort)endpoint.Port;
        }

        public NodeDescription(NativeReader reader)
        {
            NodeId = reader.ReadUInt64();
            Ip = reader.ReadASCII();
            Port = reader.ReadUInt16();
        }

        public void Serialize(NativeWriter writer)
        {
            writer.Write(NodeId);
            writer.WriteASCII(Ip);
            writer.Write(Port);
        }


        public override string ToString()
        {
            return string.Format("Node<Id:{0}, Ip:{1}, Port:{2}>", NodeId, Ip, Port);
        }
    }
}
