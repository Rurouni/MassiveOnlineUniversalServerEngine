using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;

namespace Protocol
{
    [NodeEntityContract]
    public interface IPingPonger
    {
        [NodeEntityOperation(Target = OperationTargetType.Any, IsOrdered = true, IsReliable = true)]
        int Ping(int requestId);
    }
}
