using System;
using System.Runtime.Serialization;
using RakNetWrapper;

namespace MOUSE.Core
{
    [DataContract]
    public class MessageHeader
    {}
    
    [DataContract]
    public class RoutedTransportHeader : MessageHeader
    {
        [DataMember]
        public ulong SourceNodeId;

        public RoutedTransportHeader(ulong sourceNodeId)
        {
            SourceNodeId = sourceNodeId;
        }
    }
}