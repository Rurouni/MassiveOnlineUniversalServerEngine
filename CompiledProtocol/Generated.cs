using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using RakNetWrapper;
using System.Runtime.Serialization;
using SampleProtocol;

namespace SampleDomain.Generated
{
    [NodeEntityProxy(TargetType = typeof(ISampleEntity))]
    public class ISampleEntityProxy : NodeEntityProxy, ISampleEntity
    {
        public ISampleEntityProxy(ulong entityId, Node node)
            : base(entityId, node)
        {}

        public override NodeEntityProxy New(ulong entityId, Node node)
        {
            return new ISampleEntityProxy(entityId, node);
        }

        Task<PingReply> ISampleEntity.Ping(PingRequest input)
        {
            return Node.AsyncEntityCall<PingRequest, PingReply>(input, this);
        }
        
    }
}
