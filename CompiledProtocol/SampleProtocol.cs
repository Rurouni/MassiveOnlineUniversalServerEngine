using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MOUSE.Core;
using RakNetWrapper;

namespace CompiledProtocol
{
    public class Ping: IOperation
    {
        public const ulong OperationId = 10001;

        public int RequestId;

        public Ping(int requestId)
        {
            RequestId = requestId;
        }

        public ulong Id
        {
            get
            {
                return OperationId;
            }
        }

        public OperationHeader Header
        {
            get { throw new NotImplementedException(); }
        }
    }
    

    public class Pong
    {
        public const ulong OperationId = 10002;

        public int RequestId;

        public Pong(int requestId)
        {
            RequestId = requestId;
        }

        public ulong Id
        {
            get
            {
                return OperationId;
            }
        }
    }

    public class PingPongProtocol : IProtocolDescription
    {
        public IOperation Deserialize(ulong operationId, InPacket operationData)
        {
            switch (operationId)
            {
                case Ping.OperationId: return new Ping(operationData.) ;
                case Pong.OperationId: return true;
            }
        }

        public Tuple<OperationDescription, OutPacket> Serialize(IOperation obj0)
        {
            throw new NotImplementedException();
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
