using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using System.Runtime.Serialization;

namespace SampleProtocol
{
    [NodeEntityContract]
    public interface ISampleEntity
    {
        [NodeEntityOperation]
        Task<PingReply> Ping(PingRequest input);
    }

    [DataContract]
    public class PingRequest : Message
    {
        [DataMember]
        public int RequestId;

        public PingRequest(int requestId)
        {
            RequestId = requestId;
        }
    }

    [DataContract]
    public class PingReply : Message
    {
        [DataMember]
        public int RequestId;

        public PingReply(int requestId)
        {
            RequestId = requestId;
        }
    }
}
