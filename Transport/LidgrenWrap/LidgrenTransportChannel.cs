using System;
using System.Net;
using Lidgren.Network;
using MOUSE.Core;

namespace LidgrenWrap
{
    public class LidgrenTransportChannel : ITransportChannel
    {
        readonly NetConnection _connection;
        public Action<ArraySegment<byte>> OnDataReceived { get; private set; }
        public Action OnDisconnect { get; private set; }

        public LidgrenTransportChannel(NetConnection connection)
        {
            _connection = connection;
        }

        public uint LocalId
        {
            get { return (uint)_connection.RemoteUniqueIdentifier; }
        }

        public IPEndPoint EndPoint
        {
            get { return _connection.RemoteEndPoint; }
        }

        public void Send(MessagePriority priority, MessageReliability reliability, ArraySegment<byte> data)
        {
            NetOutgoingMessage packet = _connection.Peer.CreateMessage();
            packet.Write(data.Count);
            packet.Write(data.Array, data.Offset, data.Count);

            _connection.SendMessage(packet, Convert(reliability), (int) priority);
        }

        public void Bind(Action<ArraySegment<byte>> dataReceivedAction, Action disconnectedAction)
        {
            OnDataReceived = dataReceivedAction;
            OnDisconnect = disconnectedAction;
        }

        public NetDeliveryMethod Convert(MessageReliability val)
        {
            switch (val)
            {
                case MessageReliability.Unreliable:
                    return NetDeliveryMethod.Unreliable;
                case MessageReliability.UnreliableOrdered:
                    return NetDeliveryMethod.UnreliableSequenced;
                case MessageReliability.Reliable:
                    return NetDeliveryMethod.ReliableUnordered;
                case MessageReliability.ReliableOrdered:
                    return NetDeliveryMethod.ReliableOrdered;
                default:
                    throw new ArgumentOutOfRangeException("val");
            }
        }

        public void Dispose()
        {
            _connection.Disconnect("");
        }
    }
}