using System;
using System.Runtime.Serialization;
using RakNetWrapper;

namespace MOUSE.Core
{
    [DataContract]
    public enum TransportHeaderType : byte
    {
        Direct,
        Routed
    }

    [DataContract]
    public class TransportHeader
    {
        [DataMember]
        public TransportHeaderType Id;

        public TransportHeader(TransportHeaderType id)
        {
            Id = id;
        }

    }
    [DataContract]
    public class RoutedTransportHeader : TransportHeader
    {
        [DataMember]
        public ulong SourceNodeId;

        public RoutedTransportHeader(ulong sourceNodeId)
            : base(TransportHeaderType.Routed)
        {
            SourceNodeId = sourceNodeId;
        }
    }
}