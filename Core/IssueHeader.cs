using System;
using System.Runtime.Serialization;
using RakNetWrapper;

namespace MOUSE.Core
{
    [DataContract]
    public enum IssueHeaderType : byte
    {
        Message,
        NodeMessage
    }

    [DataContract]
    public class IssueHeader
    {
        [DataMember]
        public IssueHeaderType Id;

        public IssueHeader(IssueHeaderType id)
        {
            Id = id;
        }
    }
}