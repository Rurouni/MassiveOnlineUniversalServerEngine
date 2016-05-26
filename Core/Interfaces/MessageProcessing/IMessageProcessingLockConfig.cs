using System;

namespace MOUSE.Core.Interfaces.MessageProcessing
{
    public interface IMessageProcessingLockConfig
    {
        LockType GetLockType(Type msgType);
    }

    public interface IMessageProcessingLockConfigBuilder
    {
        IMessageProcessingLockConfigBuilder Lock<TMessage>(LockType lockType) where TMessage : Message;
        IMessageProcessingLockConfig Build();
    }
}