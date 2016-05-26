using System;
using System.Threading.Tasks;

namespace MOUSE.Core.Interfaces.MessageProcessing
{
    public interface IGenericMessageProcessor
    {
        Task<Message> Process(Func<IOperationContext, Task<Message>> next, IOperationContext operation);
    }

    public interface IMessageHandler {}

    public interface IHandleMessageAsync<in TMessage> : IMessageHandler
        where TMessage : Message
    {
        Task Process(TMessage msg, IOperationContext context);
    }

    public interface IHandleRequestAsync<in TRequest, TReply> : IMessageHandler
        where TRequest : Message
        where TReply : Message
    {
        Task<TReply> Process(TRequest request, IOperationContext context);
    }

    public interface IHandleRequestAsync<in TRequest> : IMessageHandler
        where TRequest : Message
    {
        Task<Message> Process(TRequest request, IOperationContext context);
    }

    public interface IHandleMessage<in TMessage> : IMessageHandler
        where TMessage : Message
    {
        void Process(TMessage msg, IOperationContext context);
    }

    public interface IHandleRequest<in TRequest, out TReply> : IMessageHandler
        where TRequest : Message
        where TReply : Message
    {
        TReply Process(TRequest request, IOperationContext context);
    }
}
