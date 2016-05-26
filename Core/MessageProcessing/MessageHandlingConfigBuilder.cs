using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.MessageProcessing
{
    public class MessageHandlingConfigBuilder : IMessageHandlingConfigBuilder, IMessageHandlingConfig, ISimpleMessageHandlingConfigBuilder
    {
        Dictionary<Type, Func<IOperationContext, Task<Message>>> _handlerByType = new Dictionary<Type, Func<IOperationContext, Task<Message>>>();

        public IMessageHandlingConfigBuilder HandleRequestAsync<TRequest, TReply>(Func<TRequest, IOperationContext, Task<TReply>> handler)
            where TRequest : Message
            where TReply : Message
        {
            _handlerByType[typeof(TRequest)] = async (operation) => await handler(operation.Message as TRequest, operation);
            return this;
        }

        public IMessageHandlingConfigBuilder HandleMessageAsync<TRequest>(Func<TRequest, IOperationContext, Task> handler)
            where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = async (operation) =>
            {
                await handler(operation.Message as TRequest, operation);
                return null;
            };
            return this;
        }

        public IMessageHandlingConfigBuilder HandleRequest<TRequest, TReply>(Func<TRequest, IOperationContext, TReply> handler)
            where TRequest : Message
            where TReply : Message
        {
            _handlerByType[typeof(TRequest)] = (operation) =>
            {
                var msg = handler(operation.Message as TRequest, operation);
                return Task.FromResult<Message>(msg);
            };
            return this;
        }


        public IMessageHandlingConfigBuilder HandleMessage<TRequest>(Action<TRequest, IOperationContext> handler)
            where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = (operation) =>
            {
                handler(operation.Message as TRequest, operation);
                return Task.FromResult<Message>(null);
            };
            return this;
        }

        public ISimpleMessageHandlingConfigBuilder HandleMessage<TRequest>(Action<TRequest> handler) where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = (operation) =>
            {
                handler(operation.Message as TRequest);
                return Task.FromResult<Message>(null);
            };
            return this;
        }

        public ISimpleMessageHandlingConfigBuilder HandleMessageAsync<TRequest>(Func<TRequest, Task> handler) where TRequest : Message
        {
            _handlerByType[typeof(TRequest)] = async (operation) =>
            {
                await handler(operation.Message as TRequest);
                return null;
            };
            return this;
        }

        public bool TryGetHandlerFor(Type messageType, out Func<IOperationContext, Task<Message>> handler)
        {
            return _handlerByType.TryGetValue(messageType, out handler);
        }

        public IMessageHandlingConfig Build()
        {
            return this;
        }
    }
}