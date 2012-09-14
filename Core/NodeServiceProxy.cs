using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOUSE.Core
{
    public interface INodeServiceProxy
    {
        NodeServiceKey ServiceKey { get; }
        IMessageFactory MessageFactory { get; }
        IServiceOperationDispatcher Target { get; }
        NodeServiceContractDescription Description { get; }
        Task<Message> ExecuteServiceOperation(Message request);
        void ExecuteOneWayServiceOperation(Message request);

    }

    public abstract class NodeServiceProxy : INodeServiceProxy
    {
        private NodeServiceKey _serviceKey;
        private NodeServiceContractDescription _description;
        private ServiceHeader _serviceHeader;
        private IMessageFactory _messageFactory;
        private IServiceOperationDispatcher _target;

        public void Init(NodeServiceKey serviceKey, NodeServiceContractDescription description, IMessageFactory messageFactory, IServiceOperationDispatcher target)
        {
            _serviceKey = serviceKey;
            _description = description;
            _serviceHeader = new ServiceHeader(serviceKey);
            _messageFactory = messageFactory;
            _target = target;
        }

        public NodeServiceKey ServiceKey
        {
            get { return _serviceKey; }
        }

        public IMessageFactory MessageFactory
        {
            get { return _messageFactory; }
        }

        public IServiceOperationDispatcher Target
        {
            get { return _target; }
        }

        public NodeServiceContractDescription Description
        {
            get { return _description; }
        }

        public Task<Message> ExecuteServiceOperation(Message request)
        {
            request.AttachHeader(_serviceHeader);
            return _target.ExecuteServiceOperation(request);
        }

        public void ExecuteOneWayServiceOperation(Message request)
        {
            request.AttachHeader(_serviceHeader);
            _target.ExecuteOneWayServiceOperation(request);
        }
    }
}
