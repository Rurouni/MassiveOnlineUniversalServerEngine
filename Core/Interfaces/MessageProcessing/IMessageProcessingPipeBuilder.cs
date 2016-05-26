using System;
using System.Threading.Tasks;

namespace MOUSE.Core.Interfaces.MessageProcessing
{
    public interface IMessageProcessingPipeBuilder
    {
        IMessageProcessingPipeBuilder Use(Func<Func<IOperationContext, Task<Message>>, Func<IOperationContext, Task<Message>>> processorChainer);
        Func<IOperationContext, Task<Message>> Build();
    }
}