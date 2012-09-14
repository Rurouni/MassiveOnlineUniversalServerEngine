using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Globalization;

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

    [DataContract]
    public sealed class EmptyMessage : Message
    {
        public override uint Id { get { return (uint)NodeMessageId.Empty; } }
    }

    [DataContract]
    public sealed class ConnectRequest : Message
    {
        [DataMember]
        public NodeDescription Description;

        public override uint Id { get { return (uint)NodeMessageId.ConnectionRequest; } }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Description = new NodeDescription(reader);
        }
    }

    [DataContract]
    public sealed class ConnectReply : Message
    {
        [DataMember]
        public NodeDescription Description;

        public override uint Id { get { return (uint)NodeMessageId.ConnectionReply; } }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Description = new NodeDescription(reader);
        }
    }

    [DataContract]
    public sealed class ServiceAccessRequest : Message
    {
        [DataMember]
        public NodeServiceKey ServiceKey;

        public override uint Id { get { return (uint)NodeMessageId.ServiceAccessRequest; } }

        public ServiceAccessRequest(NodeServiceKey serviceKey)
        {
            ServiceKey = serviceKey;
        }

        public ServiceAccessRequest()
        {
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            ServiceKey.Serialize(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ServiceKey = new NodeServiceKey(reader);
        }
    }

    [DataContract]
    public sealed class ServiceAccessReply : Message
    {
        [DataMember]
        public bool IsValid;
        [DataMember]
        public NodeDescription ServiceOwner;

        public override uint Id { get { return (uint)NodeMessageId.ServiceAccessReply; } }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(IsValid);
            if (ServiceOwner != null)
            {
                writer.Write(true);
                ServiceOwner.Serialize(writer);
            }
            else
                writer.Write(false);

            
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            IsValid = reader.ReadBoolean();
            bool exist = reader.ReadBoolean();
            if(exist)
                ServiceOwner = new NodeDescription(reader);
        }

        public ServiceAccessReply()
        {
        }
        
        public ServiceAccessReply(bool isValid, NodeDescription serviceOwner)
        {
            IsValid = isValid;
            ServiceOwner = serviceOwner;
        }
    }

    [DataContract]
    public sealed class UpdateClusterInfo : Message
    {
        [DataMember]
        public List<NodeDescription> Descriptions;

        public override uint Id { get { return (uint)NodeMessageId.UpdateClusterInfo; } }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Descriptions.Count);
            foreach (var description in Descriptions)
                description.Serialize(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            int count = reader.ReadInt32();
            Descriptions = new List<NodeDescription>(count);
            for (int i = 0; i < count; i++)
                Descriptions.Add(new NodeDescription(reader));
        }
    }

    [DataContract]
    public sealed class EntityDiscoveryRequest : Message
    {
        public override uint Id { get { return (uint)NodeMessageId.EntityDiscoveryRequest; } }
    }

    [DataContract]
    public sealed class EntityDiscoveryReply : Message
    {
        [DataMember]
        public NodeDescription Description;

        public override uint Id { get { return (uint)NodeMessageId.EntityDiscoveryReply; } }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Description = new NodeDescription(reader);
        }
    }

    [DataContract]
    public sealed class InvalidOperation : Message
    {
        [DataMember]
        public ushort ErrorCode;
        [DataMember]
        public string DebugDescription;

        public override uint Id { get { return (uint)NodeMessageId.InvalidOperation; } }

        public InvalidOperation()
        {
            ErrorCode = 0;
            DebugDescription = null;
        }

        public InvalidOperation(ushort errorCode, string debugDescription)
        {
            ErrorCode = errorCode;
            DebugDescription = debugDescription;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ErrorCode);
            writer.Write(DebugDescription);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ErrorCode = reader.ReadUInt16();
            DebugDescription = reader.ReadString();
        }
    }

    [DataContract]
    public class NodeDescription
    {
        [DataMember]
        public readonly ulong NodeId;
        [DataMember]
        public readonly string Address;

        private IPEndPoint _endPoint;
        public IPEndPoint EndPoint
        {
            get
            {
                if(_endPoint == null)
                    _endPoint = CreateIPEndPoint(Address);
                return _endPoint;
            }
        }

        public NodeDescription(ulong nodeId, IPEndPoint endpoint)
        {
            NodeId = nodeId;
            _endPoint = endpoint;
            Address = endpoint.ToString();
        }

        public NodeDescription(BinaryReader reader)
        {
            NodeId = reader.ReadUInt64();
            Address = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NodeId);
            writer.Write(Address);
        }

        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }


        public override string ToString()
        {
            return string.Format("Node<Id:{0}, Endpoint:{1}>", NodeId, Address);
        }
    }
}
