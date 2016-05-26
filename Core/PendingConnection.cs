using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;

namespace MOUSE.Core
{
    public class PendingConnection
    {
        public readonly TaskCompletionSource<INetChannel> TCS;
        public readonly DateTime StartTime;
        public readonly IPEndPoint Target;
        public readonly CancellationTokenSource Expiration;

        public PendingConnection(IPEndPoint target)
        {
            TCS = new TaskCompletionSource<INetChannel>();
            StartTime = DateTime.Now;
            Target = target;
            Expiration = new CancellationTokenSource();
        }
    }
}