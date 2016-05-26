using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MOUSE.Core
{
    /// <summary>
    /// something that can send/receive data on the wire, abstracts full duplex communication
    /// </summary>
    public interface INetProvider : IDisposable
    {
        //action is called on network thread so you are expected to not block it with computation
        bool Init(Action<ITransportChannel> incommingConnectionAction);
        void Connect(IPEndPoint target);

        /// <returns>if caller should continue polling, false to stop</returns>
        bool PumpEvents();
        IPEndPoint EndPoint { get; }
    }

    public interface ITransportChannel : IDisposable
    {
        uint LocalId { get; }
        IPEndPoint EndPoint { get; }
        void Send(MessagePriority priority, MessageReliability reliability, ArraySegment<byte> data);

        void Bind(Action<ArraySegment<byte>> dataReceivedAction, Action disconnectedAction);
    }

    public enum MessagePriority
    {
        High = 1,
        Medium = 2,
        Low = 3
    }

    public enum MessageReliability
    {
        Unreliable = 1,
        UnreliableOrdered = 2,
        Reliable = 3,
        ReliableOrdered = 4
    }
}
