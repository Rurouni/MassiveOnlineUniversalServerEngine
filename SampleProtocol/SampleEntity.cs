using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using System.Runtime.Serialization;
using RakNetWrapper;

namespace SampleProtocol
{
    [NodeEntityContract]
    public interface ISampleEntity
    {
        [NodeEntityOperation]
        Task<int> Ping(int requestId);
    }
}
