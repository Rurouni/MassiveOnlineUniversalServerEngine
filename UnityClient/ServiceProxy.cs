using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;

namespace UnityClient
{
    public class NodeServiceContractDescription
    {
        public readonly uint TypeId;
        public readonly Type ContractType;
        public readonly Type ProxyType;
        public readonly List<NodeServiceOperationDescription> Operations;
        public readonly NetContractAttribute Attribute;

        public NodeServiceContractDescription(uint typeId, Type contractType, Type proxyType,
            NetContractAttribute contractAttribute, List<NodeServiceOperationDescription> operations)
        {
            TypeId = typeId;
            ContractType = contractType;
            ProxyType = proxyType;
            Attribute = contractAttribute;
            Operations = operations;
        }

        public bool AllowExternalConnections
        {
            get { return Attribute.AllowExternalConnections; }
        }
    }

    public class NodeServiceOperationDescription
    {
        public readonly string Name;
        public readonly uint RequestMessageId;
        public readonly uint? ReplyMessageId;
        public readonly Action<IMessageFactory, object, Message> Dispatch;

        public NodeServiceOperationDescription(string name, uint requestMessageId, uint? replyMessageId, Action<IMessageFactory, object, Message> dispatch)
        {
            Name = name;
            RequestMessageId = requestMessageId;
            ReplyMessageId = replyMessageId;
            Dispatch = dispatch;
        }
    }

    public interface INodeServiceProxy
    {
        NodeServiceKey ServiceKey { get; }
        IMessageFactory MessageFactory { get; }
        INetClient Client { get; }
        NodeServiceContractDescription Description { get; }
        IEnumerable ExecuteServiceOperation(Message request, OperationReply<Message> reply);
        void ExecuteOneWayServiceOperation(Message request);

    }

    public abstract class NodeServiceProxy : INodeServiceProxy
    {
        private NodeServiceKey _serviceKey;
        private NodeServiceContractDescription _description;
        private ServiceHeader _serviceHeader;
        private IMessageFactory _messageFactory;
        private INetClient _client;

        public void Init(NodeServiceKey serviceKey, NodeServiceContractDescription description, IMessageFactory messageFactory, INetClient client)
        {
            _serviceKey = serviceKey;
            _description = description;
            _serviceHeader = new ServiceHeader(serviceKey);
            _messageFactory = messageFactory;
            _client = client;
        }

        public NodeServiceKey ServiceKey
        {
            get { return _serviceKey; }
        }

        public IMessageFactory MessageFactory
        {
            get { return _messageFactory; }
        }

        public INetClient Client
        {
            get { return _client; }
        }

        public NodeServiceContractDescription Description
        {
            get { return _description; }
        }

        public IEnumerable ExecuteServiceOperation(Message request, OperationReply<Message> reply)
        {
            request.AttachHeader(_serviceHeader);
            return _client.ExecuteOperation(request, reply);
        }

        public void ExecuteOneWayServiceOperation(Message request)
        {
            request.AttachHeader(_serviceHeader);
            _client.ExecuteOperation(request);
        }
    }
}
