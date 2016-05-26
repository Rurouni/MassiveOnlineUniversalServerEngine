using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MOUSE.Core
{
    public class ConnectionFailedException : Exception
    {
        public IPEndPoint Endpoint { get; private set; }

        public ConnectionFailedException(IPEndPoint endpoint)
            : base("Cant connect to -" + endpoint)
        {
            Endpoint = endpoint;
        }
    }

    public class ProcessingException : Exception
    {
        public ushort ErrorCode;

        public ProcessingException(Enum errorCode, string debugMessage)
            : base(debugMessage)
        {
            ErrorCode = Convert.ToUInt16(errorCode);
        }

        public ProcessingException(ushort errorCode, string debugMessage)
            : base(debugMessage)
        {
            ErrorCode = errorCode;
        }

        public ProcessingException(Enum errorCode)
            : base("Processing Failed:" + errorCode)
        {
            ErrorCode = Convert.ToUInt16(errorCode);
        }
    }
}
