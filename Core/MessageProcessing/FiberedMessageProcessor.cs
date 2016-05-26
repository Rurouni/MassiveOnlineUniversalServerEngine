using System;
using System.Threading.Tasks;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Fibers;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.MessageProcessing
{
    public class FiberedMessageProcessor : IGenericMessageProcessor
    {
        readonly IFiber _fiber;
        readonly IMessageProcessingLockConfig _lockConfig;

        public FiberedMessageProcessor(IFiber fiber, IMessageProcessingLockConfig lockConfig = null)
        {
            _fiber = fiber;
            _lockConfig = lockConfig;
        }

        public Task<Message> Process(Func<IOperationContext, Task<Message>> next, IOperationContext operation)
        {
            var lockType = LockType.Write;
            if (_lockConfig != null)
                lockType = _lockConfig.GetLockType(operation.Message.GetType());

            return _fiber.Call(() => next(operation), lockType);
        }
    }
}