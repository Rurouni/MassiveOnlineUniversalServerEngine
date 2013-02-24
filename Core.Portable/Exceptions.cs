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
}
