using System;
using System.Threading.Tasks;

namespace MOUSE.Core.Interfaces.MessageProcessing
{
    public interface IMessageHandlingConfigBuilder
    {
        IMessageHandlingConfigBuilder HandleRequestAsync<TRequest, TReply>(Func<TRequest, IOperationContext, Task<TReply>> handler)
            where TRequest : Message
            where TReply : Message;

        IMessageHandlingConfigBuilder HandleRequest<TRequest, TReply>(Func<TRequest, IOperationContext, TReply> handler)
            where TRequest : Message
            where TReply : Message;

        IMessageHandlingConfigBuilder HandleMessageAsync<TRequest>(Func<TRequest, IOperationContext, Task> handler) where TRequest : Message;
        IMessageHandlingConfigBuilder HandleMessage<TRequest>(Action<TRequest, IOperationContext> handler) where TRequest : Message;

        IMessageHandlingConfig Build();
    }

    public interface IMessageHandlingConfig
    {
        bool TryGetHandlerFor(Type messageType, out Func<IOperationContext, Task<Message>> handler);
    }

    public interface ISimpleMessageHandlingConfigBuilder
    {
        ISimpleMessageHandlingConfigBuilder HandleMessage<TRequest>(Action<TRequest> handler) where TRequest : Message;
        ISimpleMessageHandlingConfigBuilder HandleMessageAsync<TRequest>(Func<TRequest, Task> handler) where TRequest : Message;
        IMessageHandlingConfig Build();
    }
}