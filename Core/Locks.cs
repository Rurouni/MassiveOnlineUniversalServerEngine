using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    public enum LockType
    {
        None, //dispatch will be fired in thread pool thread
        Read, //dispatch will be fired in Concurrent Scheduler
        Write //dispatch  will be fired in Exclusive Scheduler
    }
}
