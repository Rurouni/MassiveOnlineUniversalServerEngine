using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;

namespace Protocol
{
    [NodeEntityContract]
    public interface IChatOperations
    {
        [NodeEntityOperation]
        void Enter(long accountId);

        [NodeEntityOperation]
        void Say(string text);

        [NodeEntityOperation]
        void Leave();
    }
}
