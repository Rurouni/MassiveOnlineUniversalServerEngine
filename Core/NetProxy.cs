using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core.Actors;

namespace MOUSE.Core
{
    public interface INetProxy
    {
        ActorKey? Actor { get; set; }
        IMessageFactory MessageFactory { get; }
        IOperationExecutor Target { get; }
        NetContractDescription Description { get; }
    }

    public abstract class NetProxy : INetProxy
    {
        private ActorKey? _actor;
        private NetContractDescription _description;
        private ActorHeader _actorHeader;
        private IMessageFactory _messageFactory;
        private IOperationExecutor _target;

        public void Init(NetContractDescription description, IMessageFactory messageFactory, IOperationExecutor target, ActorKey? actor = null)
        {
            _description = description;
            _messageFactory = messageFactory;
            _target = target;
            Actor = actor;
        }

        public ActorKey? Actor
        {
            get { return _actor; }
            set
            {
                _actorHeader = value != null ? new ActorHeader(value.Value.LocalActorId) : null;
                _actor = value;
            }
        }

        public IMessageFactory MessageFactory
        {
            get { return _messageFactory; }
        }

        public IOperationExecutor Target
        {
            get { return _target; }
        }

        public NetContractDescription Description
        {
            get { return _description; }
        }

        public Task<Message> ExecuteOperation(Message msg)
        {
            if (_actorHeader != null)
            {
                msg.AttachHeader(_actorHeader);
            }
            return _target.ExecuteOperation(new OperationContext(msg, null));
        }

        public void ExecuteOneWayOperation(Message msg)
        {
            if (_actorHeader != null)
            {
                msg.AttachHeader(_actorHeader);
            }
            _target.ExecuteOneWayOperation(new OperationContext(msg, null));
        }
    }
}
