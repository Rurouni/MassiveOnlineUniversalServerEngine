using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using SampleProtocol;

namespace SampleServer
{
    [NodeEntity]
    public class SampleEntity : ISampleEntity
    {
        public async Task<PingReply> Ping(PingRequest input)
        {
            return new PingReply(input.RequestId);
        }
    }
}
