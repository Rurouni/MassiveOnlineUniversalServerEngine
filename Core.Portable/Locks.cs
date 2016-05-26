using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    public enum LockType
    {
        /// <summary>
        /// processing will happen in network receiver thread, use for hardcore optimizations only
        /// </summary>
        None, 
        /// <summary>
        /// processing will happen in fiber allowing other operation with Read level to coexecute simultaneously
        /// </summary>
        Read, 
        /// <summary>
        /// processing will happen in fiber taking exclusive access until operation is fully complete
        /// </summary>
        Write 
    }
}
