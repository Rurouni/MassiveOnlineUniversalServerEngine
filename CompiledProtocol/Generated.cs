using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.FSharp.Control;
using MOUSE.Core;
using RakNetWrapper;
using SampleDomain;
using SampleDomain.Interfaces;

namespace SampleDomain.Generated
{
    public class PingRequest: IOperation
    {
        public int requestId;

        public PingRequest()
        {}

        public PingRequest(InPacket packet)
        {
            requestId = packet.ReadInt32();
        }

        public void Serialize(OutPacket packet)
        {
            packet.WriteInt32(requestId);
        }

        public const ulong OperationId = 10001;
        private OperationHeader _header = new OperationHeader(OperationId);
        private readonly OperationDescription _description = new OperationDescription(OperationPriority.Medium, OperationReliability.ReliableOrdered);

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

    public class PingReply: IOperation
    {
        public int RequestId;

        public PingReply(InPacket packet)
        {
            RequestId = packet.ReadInt32();
        }

        public void Serialize(OutPacket packet)
        {
            packet.WriteInt32(RequestId);
        }

        public const ulong OperationId = 10002;
        private OperationHeader _header = new OperationHeader(OperationId);
        private readonly OperationDescription _description = new OperationDescription(OperationPriority.Immediate, OperationReliability.ReliableOrdered);
        

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

    public class DomainProtocol : IProtocolDescription
    {
        public IOperation Deserialize(ulong operationId, InPacket operationData)
        {
            switch (operationId)
            {
                case PingRequest.OperationId: return new PingRequest(operationData);
                case PingReply.OperationId: return new PingReply(operationData);

                default: throw new Exception("Cant deserialize OperationId:" + operationId);
            }
        }

        public void Serialize(IOperation operation, OutPacket packet)
        {
            switch (operation.Header.OperationId)
            {
                case PingRequest.OperationId: (operation as PingRequest).Serialize(packet); break;
                case PingReply.OperationId: (operation as PingReply).Serialize(packet); break;

                default: throw new Exception("Cant serialize " + operation);
            }
        }

        public bool Contains(ulong operationId)
        {
            switch (operationId)
            {
                case PingRequest.OperationId: return true;
                case PingReply.OperationId: return true;
            }
            return false;
        }
    }

    public class PingerProxy : IPinger
    {
        private INode _host;
        private EntityOperationDispatcher _dispatcher;

        public PingerProxy(INode host, IOperationDispatcher dispatcher)
        {
            _host = host;
            _dispatcher = (EntityOperationDispatcher) dispatcher;
        }

        public FSharpAsync<int> Ping(int requestId)
        {
            var operation = new PingRequest();
            operation.requestId = requestId;
            return _dispatcher.SendAndAwaitOperation<PingReply, PingRequest>()
            _host.Execute(operation);
            FSharpAsync.
            
        }
    }

    public class
}
