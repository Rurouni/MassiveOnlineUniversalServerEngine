using System;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Fibers;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.MessageProcessing
{
    public static class MessageProcessorExtensions
    {
        public static IMessageProcessingPipeBuilder UseConfigurableDispatcher(this IMessageProcessingPipeBuilder builder, Func<IMessageHandlingConfigBuilder, IMessageHandlingConfigBuilder> configure,
            bool throwIfUnhandled = true)
        {
            var config = new MessageHandlingConfigBuilder();
            var mp = new DispatchingMessageProcessor(configure(config).Build(), throwIfUnhandled);
            
            return builder.Use(next => context => mp.Process(next, context));
        }

        public static IMessageProcessingPipeBuilder UseConfigurableClientDispatcher(this IMessageProcessingPipeBuilder builder,
            Func<ISimpleMessageHandlingConfigBuilder, ISimpleMessageHandlingConfigBuilder> configure)
        {
            var config = new MessageHandlingConfigBuilder();
            var mp = new DispatchingMessageProcessor(configure(config).Build(), false);
            return builder.Use(next => context => mp.Process(next, context));
        }

        public static IMessageProcessingPipeBuilder UseFiber(this IMessageProcessingPipeBuilder builder, IFiber fiber,
            Func<IMessageProcessingLockConfigBuilder, IMessageProcessingLockConfigBuilder> lockConfigurator = null)
        {
            IMessageProcessingLockConfig lockConfig = lockConfigurator != null ? lockConfigurator(new MessageProcessorLockConfig()).Build() : new MessageProcessorLockConfig();
            var mp = new FiberedMessageProcessor(fiber, lockConfig);
            return builder.Use(next => context => mp.Process(next, context));
        }

        public static IMessageProcessingPipeBuilder UseIdleDisconnect(this IMessageProcessingPipeBuilder builder, TimeSpan idleDisconnectTimeout, INetChannel channel)
        {
            var mp = new IdleDisconnectProcessor(idleDisconnectTimeout, channel);
            return builder.Use(next => context => mp.Process(next, context));
        }
    }
}