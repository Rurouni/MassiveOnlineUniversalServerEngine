using System;
using System.Collections.Generic;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.MessageProcessing
{
    public class MessageProcessorLockConfig : IMessageProcessingLockConfig, IMessageProcessingLockConfigBuilder
    {
        readonly Dictionary<Type, LockType> _lockTypeByMessageType = new Dictionary<Type, LockType>();

        public IMessageProcessingLockConfigBuilder Lock<TMessage>(LockType lockType)
            where TMessage : Message
        {
            _lockTypeByMessageType.Add(typeof(TMessage), lockType);
            return this;
        }

        public LockType GetLockType(Type msgType)
        {
            LockType lockType;
            if (_lockTypeByMessageType.TryGetValue(msgType, out lockType))
                return lockType;
            else
                return LockType.Write;
        }

        public IMessageProcessingLockConfig Build()
        {
            return this;
        }
    }
}