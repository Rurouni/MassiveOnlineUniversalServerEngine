﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    public enum BasicErrorCode : ushort
    {
        Unknown = 0,
        ConnectionFailed = 1,
        OperationTimeout = 2,
        DispatcherFuncNotFound = 3,
        NonExistentActor = 4,
        ActorSystemNotFound = 5,
        
        

        /// <summary>
        /// should be used as start for custom errors
        /// </summary>
        Last = 100,
    }
}
