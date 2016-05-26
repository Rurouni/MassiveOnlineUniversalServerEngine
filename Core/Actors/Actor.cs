using System;
using System.Threading;
using System.Threading.Tasks;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Fibers;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.MessageProcessing;

namespace MOUSE.Core.Actors
{
    public abstract class Actor : IActor
    {
        Func<IOperationContext, Task<Message>> _processor;
        IMessageProcessingPipeBuilder _pipeBuilder;
        
        protected IFiber Fiber { get; private set; }

        public IActorSystem MySystem { get; private set; }

        public ActorRef ActorRef { get; private set; }

        void IActor.Init(IActorSystem system, ActorRef actorRef)
        {
            MySystem = system;
            ActorRef = actorRef;
            OnInitialise();
        }
        
        protected void OnInitialise()
        {
            _pipeBuilder = new MessageProcessingPipeBuilder();
            Fiber = new ReadWriteLockingFiber();
            ConfigurePipe(_pipeBuilder);
            _processor = _pipeBuilder.Build();
        }

        protected virtual IMessageProcessingPipeBuilder ConfigurePipe(IMessageProcessingPipeBuilder builder)
        {
            return builder
                .UseFiber(Fiber, ConfigureLocks)
                .UseConfigurableDispatcher(ConfigureHandlers);
        }
        protected virtual IMessageProcessingLockConfigBuilder ConfigureLocks(IMessageProcessingLockConfigBuilder builder)
        {
            return builder;
        }

        protected abstract IMessageHandlingConfigBuilder ConfigureHandlers(IMessageHandlingConfigBuilder builder);

        public Task<Message> Process(IOperationContext operation)
        {
            return _processor(operation);
        }

        public void Dispose()
        {
            Fiber.Stop();

            MySystem.DisposeActor(ActorRef.Key);
        }
    }
}