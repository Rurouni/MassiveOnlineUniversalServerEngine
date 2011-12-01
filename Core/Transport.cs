using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace MOUSE.Core
{
    public interface INetProvider
    {
        bool Init(INetPeerFactory factory);
        void Shutdown();
        void Connect(IPEndPoint target);
        bool PumpEvents();
        IPEndPoint EndPoint{get;}
    }

    public interface INetPeerFactory
    {
        INetChannelListener OnNetConnect(INetChannel channel);
    }

    /// <summary>
    /// assumes sequential calling
    /// </summary>
    public interface INetChannelListener
    {
        void OnNetData(NativeReader reader);
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
