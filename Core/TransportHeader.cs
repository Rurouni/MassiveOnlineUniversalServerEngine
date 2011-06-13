using System;
using RakNetWrapper;

namespace Core
{
    public enum TransportHeaderType : byte
    {
        Direct,
        Routed
    }

    public class TransportHeader
    {
        public TransportHeaderType Id;

        public TransportHeader(TransportHeaderType id)
        {
            Id = id;
        }

        public virtual void Serialize(OutPacket packet)
        {
            packet.WriteUByte((byte)Id);
        }

        public static TransportHeader Deserialize(InPacket packet)
        {
            var type = (TransportHeaderType)packet.ReadUByte();
            switch (type)
            {
                case TransportHeaderType.Direct:
                    return new TransportHeader(TransportHeaderType.Direct);
                    break;
                case TransportHeaderType.Routed:
                    return new RoutedTransportHeader(packet);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class RoutedTransportHeader : TransportHeader
    {
        public ulong SourceNodeId;

        public RoutedTransportHeader(ulong sourceNodeId)
            : base(TransportHeaderType.Routed)
        {
            SourceNodeId = sourceNodeId;
        }

        public RoutedTransportHeader(InPacket packet)
            : base(TransportHeaderType.Routed)
        {
            SourceNodeId = packet.ReadUInt64();
        }

        public override void Serialize(OutPacket packet)
        {
            base.Serialize(packet);
            packet.WriteUInt64(SourceNodeId);
        }
    }
}