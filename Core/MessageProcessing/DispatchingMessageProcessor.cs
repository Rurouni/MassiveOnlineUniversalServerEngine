using System;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.MessageProcessing
{
    public class DispatchingMessageProcessor : IGenericMessageProcessor
    {
        readonly IMessageHandlingConfig _messageHandlingConfig;
        readonly bool _throwIfUnhandled;
        

        public DispatchingMessageProcessor(IMessageHandlingConfig messageHandlingConfig, bool throwIfUnhandled)
        {
            _messageHandlingConfig = messageHandlingConfig;
            _throwIfUnhandled = throwIfUnhandled;
        }

        public Task<Message> Process(Func<IOperationContext, Task<Message>> next, IOperationContext context)
        {
            Func<IOperationContext, Task<Message>> handler;
            if (_messageHandlingConfig.TryGetHandlerFor(context.Message.GetType(), out handler))
            {
                return handler(context);
            }
            else if (next != null)
            {
                return next(context);
            }
            else if (_throwIfUnhandled)
            {
                throw new ProcessingException(BasicErrorCode.DispatcherFuncNotFound);
            }
            else
            {
                return Task.FromResult<Message>(null);
            }
        }
    }
}
