using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace MOUSE.Core
{
    public interface INetPeer
    {
        bool Startup(IPEndPoint listenEndpoint, int maxConnections);
        void Shutdown();
        void Connect(IPEndPoint target);
        void CloseConnection(int netIndex);
        bool ProcessNetEvent(INetEventProcessor processor);
        void Send(int netId, byte[] buff, int length, MessagePriority messagePriority, MessageReliability messageReliability);
        void SendLoopback(byte[] buff, int length);
        IPEndPoint GetEndPointOf(int netId);
        IPEndPoint EndPoint{get;}
    }

    public interface INetEventProcessor
    {
        void OnNetConnectionAccepted(int netIndex);
        void OnNetConnect(int netIndex);
        void OnNetDisconnect(int netIndex);
        void OnNetData(int netIndex, NativeReader reader);
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
