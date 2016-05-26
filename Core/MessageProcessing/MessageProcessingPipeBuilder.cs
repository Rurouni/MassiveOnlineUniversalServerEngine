using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.MessageProcessing;


namespace MOUSE.Core.MessageProcessing
{
    using Processor = Func<IOperationContext, Task<Message>>;

    public class MessageProcessingPipeBuilder : IMessageProcessingPipeBuilder
    {
        readonly LinkedList<Func<Processor, Processor>> _processorChainers = new LinkedList<Func<Processor, Processor>>();
        public IMessageProcessingPipeBuilder Use(Func<Processor, Processor> processorChainer)
        {
            _processorChainers.AddFirst(processorChainer);
            return this;
        }

        public Func<IOperationContext, Task<Message>> Build()
        {
            //fold right
            return _processorChainers.Aggregate<Func<Processor, Processor>, Processor>(null, (processor, processorChainer) => processorChainer(processor));
        }
    }

}