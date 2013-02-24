using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;
using MOUSE.Core.Actors;

namespace MOUSE.Unity
{
    public interface INetProxy
    {
        ActorKey? Actor { get; set; }
        IMessageFactory MessageFactory { get; }
        INetPeer Channel { get; }
        NetContractDescription Description { get; }
        Future<Message> ExecuteOperation(Message request, Action<Message> onResult, Action<ushort, string> onError);
        void ExecuteOneWayOperation(Message request);

    }

    public abstract class NetProxy : INetProxy
    {
        private ActorKey? _actor;
        private ActorHeader _actorHeader;
        private NetContractDescription _description;
        private IMessageFactory _messageFactory;

        public void Init(uint serviceKey, NetContractDescription description, IMessageFactory messageFactory, INetPeer channel, ActorKey? actor = null)
        {
            Actor = actor;
            _description = description;
            _messageFactory = messageFactory;
            Channel = channel;
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

        public INetPeer Channel { get; private set; }

        public NetContractDescription Description
        {
            get { return _description; }
        }

        public Future<Message> ExecuteOperation(Message msg, Action<Message> onResult, Action<ushort, string> onError)
        {
            if (_actorHeader != null)
            {
                msg.AttachHeader(_actorHeader);
            }
            return Channel.ExecuteOperation(msg, onResult, onError);
        }

        public void ExecuteOneWayOperation(Message msg)
        {
            if (_actorHeader != null)
            {
                msg.AttachHeader(_actorHeader);
            }
            Channel.ExecuteOneWayOperation(msg);
        }
    }
}
