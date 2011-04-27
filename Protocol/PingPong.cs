using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FSharp.Control;
using MOUSE.Core;

namespace Protocol
{
    [NodeEntityContract]
    public interface IPinger
    {
        [NodeEntityOperation(Target = OperationTargetType.Any,
            Reliability = OperationReliability.ReliableOrdered,
            Priority = OperationPriority.Medium)]
        int Ping(int requestId);
    }
}
