using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MOUSE.Core;
using RakNetWrapper;

namespace Protocol.Generated
{
    public class Ping: IOperation
    {
        public int RequestId;

        public Ping(int requestId)
        {
            RequestId = requestId;
        }

        public Ping(InPacket packet)
        {
            RequestId = packet.ReadInt32();
        }

        public void Serialize(OutPacket packet)
        {
            packet.WriteInt32(RequestId);
        }

        public const ulong OperationId = 10001;
        private OperationHeader _header = new OperationHeader(OperationId);
        private readonly OperationDescription _description = new OperationDescription(OperationPriority.Medium, OperationReliability.Unreliable);

        public OperationHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        public OperationContext Context { get; set; }

        public OperationDescription Description
        {
            get { return _description; }
        }
    }

    public class Pong: IOperation
    {
        public int RequestId;

        public Pong(int requestId)
        {
            RequestId = requestId;
        }

        public Pong(InPacket packet)
        {
            RequestId = packet.ReadInt32();
        }

        public void Serialize(OutPacket packet)
        {
            packet.WriteInt32(RequestId);
        }

        public const ulong OperationId = 10002;
        private OperationHeader _header = new OperationHeader(OperationId);
        private readonly OperationDescription _description = new OperationDescription(OperationPriority.Medium, OperationReliability.Unreliable);
        

        public OperationHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        public OperationContext Context { get; set; }

        public OperationDescription Description
        {
            get { return _description; }
        }
    }

    public class PingPongProtocol : IProtocolDescription
    {
        public IOperation Deserialize(ulong operationId, InPacket operationData)
        {
            switch (operationId)
            {
                case Ping.OperationId: return new Ping(operationData);
                case Pong.OperationId: return new Pong(operationData);

                default: throw new Exception("Cant deserialize OperationId:" + operationId);
            }
        }

        public void Serialize(IOperation operation, OutPacket packet)
        {
            switch (operation.Header.OperationId)
            {
                case Ping.OperationId: (operation as Ping).Serialize(packet); break;
                case Pong.OperationId: (operation as Pong).Serialize(packet); break;

                default: throw new Exception("Cant serialize " + operation);
            }
        }

        public bool Contains(ulong operationId)
        {
            switch (operationId)
            {
                case Ping.OperationId: return true;
                case Pong.OperationId: return true;
            }
            return false;
        }
    }
}
