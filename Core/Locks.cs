using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    //Stub for future locks management system
    public enum LockType
    {
        None, //dispatch will be fired in thread pool thread
        ReadReentrant, //dispatch and all continuation will be fired in Concurrent Scheduler
        WriteReentrant, //dispatch and all continuations will be fired in Exclusive Scheduler
        Full //no operations processed simultaniously(even async)
    }
}
