using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MOUSE.Core
{
    public interface INetProvider
    {
        bool Init(INetChannelConsumer factory);
        void Shutdown();
        void Connect(IPEndPoint target);
        
        /// <returns>if caller should continue polling, false to stop</returns>
        bool PumpEvents();
        IPEndPoint EndPoint{get;}
    }

    public interface INetChannelConsumer
    {
        INetChannelListener OnNetConnect(INetChannel channel);
    }

    /// <summary>
    /// methods are called on network receiving thread and you should not delay BinaryReader usage
    /// </summary>
    public interface INetChannelListener
    {
        void OnNetData(BinaryReader reader);
        void OnDisconnected();
    }

    public interface INetChannel
    {
        uint Id { get; }
        IPEndPoint EndPoint { get; }
        void Send(Message msg);
        void Close();
    }

    public enum MessagePriority
    {
        High,
        Medium,
        Low
    }

    public enum MessageReliability
    {
        Unreliable,
        UnreliableOrdered,
        Reliable,
        ReliableOrdered
    }
}
