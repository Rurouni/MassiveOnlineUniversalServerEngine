using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MOUSE.Core
{
    [DataContract]
    public abstract class MessageHeader
    {
        public static MessageHeader Deserialize(NativeReader reader)
        {
            byte headerId = reader.ReadByte();
            switch (headerId)
            {
                case 1: return new TransportHeader(reader);
                case 2: return new EntityOperationRequest(reader);
                case 3: return new EntityOperationReply(reader);
                case 4: return new PropagateConnectionHeader(reader);
                case 5: return new UpdateEntityRoutingHeader(reader);
                default: throw new Exception("Not supported header id:" + headerId);
            }
        }

        public static void Serialize(MessageHeader header, NativeWriter writer)
        {
            if (header is TransportHeader) writer.Write((byte)1);
            else if (header is EntityOperationRequest) writer.Write((byte)2);
            else if (header is EntityOperationReply) writer.Write((byte)3);
            else if (header is PropagateConnectionHeader) writer.Write((byte)4);
            else if (header is UpdateEntityRoutingHeader) writer.Write((byte)5);
            else
                throw new Exception("Not supported header id:" + header);

            header.Serialize(writer);
        }

        public abstract void Serialize(NativeWriter writer);
    }

    [DataContract]
    public class TransportHeader : MessageHeader
    {
        [DataMember]
        public readonly ulong SourceNodeId;
        [DataMember]
        public readonly ulong? RoutedNodeId;

        public TransportHeader(ulong sourceNodeId, ulong? routedNodeId)
        {
            SourceNodeId = sourceNodeId;
            RoutedNodeId = routedNodeId;
        }

        public TransportHeader(NativeReader reader)
        {
            SourceNodeId = reader.ReadUInt64();
            bool hasValue = reader.ReadBoolean();
            if (hasValue)
                RoutedNodeId = reader.ReadUInt64();
        }

        public override void Serialize(NativeWriter writer)
        {
            writer.Write(SourceNodeId);
            writer.Write(RoutedNodeId.HasValue);
            if (RoutedNodeId.HasValue)
                writer.Write(RoutedNodeId.Value);
        }
    }

    [DataContract]
    public class EntityOperationRequest : MessageHeader
    {
        [DataMember]
        public readonly uint RequestId;
        [DataMember]
        public readonly ulong TargetEntityId;

        public EntityOperationRequest(uint requestId, ulong targetEntityId)
        {
            RequestId = requestId;
            TargetEntityId = targetEntityId;
        }

        public EntityOperationRequest(NativeReader reader)
        {
            RequestId = reader.ReadUInt32();
            TargetEntityId = reader.ReadUInt64();
        }

        public override void Serialize(NativeWriter writer)
        {
            writer.Write(RequestId);
            writer.Write(TargetEntityId);
        }
    }

    [DataContract]
    public class EntityOperationReply : MessageHeader
    {
        [DataMember]
        public readonly uint RequestId;

        public EntityOperationReply(uint requestId)
        {
            RequestId = requestId;
        }

        public EntityOperationReply(NativeReader reader)
        {
            RequestId = reader.ReadUInt32();
        }

        public override void Serialize(NativeWriter writer)
        {
            writer.Write(RequestId);
        }
    }

    [DataContract]
    public class PropagateConnectionHeader : MessageHeader
    {
        [DataMember]
        public readonly NodeDescription Description;

        public PropagateConnectionHeader(NodeDescription description)
        {
            Description = description;
        }

        public PropagateConnectionHeader(NativeReader reader)
        {
            Description = new NodeDescription(reader);
        }

        public override void Serialize(NativeWriter writer)
        {
            Description.Serialize(writer);
        }
    }

    [DataContract]
    public class UpdateEntityRoutingHeader : MessageHeader
    {
        [DataMember]
        public readonly ulong EntityId;
        [DataMember]
        public readonly ulong OwnerNodeId;

        public UpdateEntityRoutingHeader(ulong entityId, ulong ownerNodeId)
        {
            EntityId = entityId;
            OwnerNodeId = ownerNodeId;
        }

        public UpdateEntityRoutingHeader(NativeReader reader)
        {
            EntityId = reader.ReadUInt64();
            OwnerNodeId = reader.ReadUInt64();
        }

        public override void Serialize(NativeWriter writer)
        {
            writer.Write(EntityId);
            writer.Write(OwnerNodeId);
        }
    }
}
