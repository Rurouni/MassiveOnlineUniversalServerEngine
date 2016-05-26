using System;
using System.Threading.Tasks;

namespace MOUSE.Core.Interfaces.MessageProcessing
{
    public interface IHandleMessagesWithLocksBuilder
    {
        IHandleMessagesWithLocksBuilder HandleRequestAsync<TRequest>(Func<IOperationContext, TRequest, Task<Message>> handler, LockType lockType = LockType.Write) where TRequest : Message;
        IHandleMessagesWithLocksBuilder HandleRequestAsync<TRequest, TReply>(Func<IOperationContext, TRequest, Task<TReply>> handler, LockType lockType = LockType.Write)
            where TRequest : Message
            where TReply : Message;
        IHandleMessagesWithLocksBuilder HandleRequest<TRequest>(Func<IOperationContext, TRequest, Message> handler, LockType lockType = LockType.Write) where TRequest : Message;
        IHandleMessagesWithLocksBuilder HandleMessageAsync<TRequest>(Func<IOperationContext, TRequest, Task> handler, LockType lockType = LockType.Write) where TRequest : Message;
        IHandleMessagesWithLocksBuilder HandleMessage<TRequest>(Action<IOperationContext, TRequest> handler, LockType lockType = LockType.Write) where TRequest : Message;
    }
}