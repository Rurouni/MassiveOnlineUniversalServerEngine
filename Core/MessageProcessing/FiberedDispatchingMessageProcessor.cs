using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Fibers;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.MessageProcessing
{
    public class FiberedDispatchingMessageProcessor : IHandleMessagesWithLocksBuilder, IGenericMessageProcessor
    {
        readonly IFiber _fiber;
        readonly bool _throwIfUnhandled;
        readonly Dictionary<Type, LockedOperationDesc> _handlerByType = new Dictionary<Type, LockedOperationDesc>();

        public FiberedDispatchingMessageProcessor(IFiber fiber, bool throwIfUnhandled)
        {
            _fiber = fiber;
            _throwIfUnhandled = throwIfUnhandled;
        }

        public Task<Message> Process(Func<IOperationContext, Task<Message>> next, IOperationContext context)
        {
            LockedOperationDesc handler;
            if (_handlerByType.TryGetValue(context.Message.GetType(), out handler))
            {
                return _fiber.Call(() => handler.ProcessFunc(context), handler.Lock);
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

        public IHandleMessagesWithLocksBuilder HandleRequestAsync<TRequest>(Func<IOperationContext, TRequest, Task<Message>> handler, LockType lockType = LockType.Write)
            where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = new LockedOperationDesc(
                (operation) => handler(operation, operation.Message as TRequest),
                lockType);
            return this;
        }

        public IHandleMessagesWithLocksBuilder HandleRequestAsync<TRequest, TReply>(Func<IOperationContext, TRequest, Task<TReply>> handler, LockType lockType = LockType.Write)
            where TRequest : Message
            where TReply : Message
        {
            _handlerByType[typeof(TRequest)] = new LockedOperationDesc( async (operation) => await handler(operation, operation.Message as TRequest), lockType);
            return this;
        }

        public IHandleMessagesWithLocksBuilder HandleMessageAsync<TRequest>(Func<IOperationContext, TRequest, Task> handler, LockType lockType = LockType.Write)
            where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = new LockedOperationDesc( async (operation) =>
            {
                await handler(operation, operation.Message as TRequest);
                return null;
            }, lockType);
            return this;
        }

        public IHandleMessagesWithLocksBuilder HandleRequest<TRequest>(Func<IOperationContext, TRequest, Message> handler, LockType lockType = LockType.Write)
            where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = new LockedOperationDesc( (operation) =>
            {
                var msg = handler(operation, operation.Message as TRequest);
                return Task.FromResult(msg);
            }, lockType);
            return this;
        }

        public IHandleMessagesWithLocksBuilder HandleMessage<TRequest>(Action<IOperationContext, TRequest> handler, LockType lockType = LockType.Write)
            where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = new LockedOperationDesc( (operation) =>
            {
                handler(operation, operation.Message as TRequest);
                return Task.FromResult<Message>(null);
            }, lockType);
            return this;
        }
    }

    public class LockedOperationDesc
    {
        readonly public Func<IOperationContext, Task<Message>> ProcessFunc;
        readonly public LockType Lock;
        public LockedOperationDesc(Func<IOperationContext, Task<Message>> processFunc, LockType @lock)
        {
            ProcessFunc = processFunc;
            Lock = @lock;
        }
    }
}