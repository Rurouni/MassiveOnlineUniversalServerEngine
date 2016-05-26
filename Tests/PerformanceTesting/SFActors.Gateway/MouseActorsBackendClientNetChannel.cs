using System.Threading.Tasks;
using Metrics;
using MOUSE.Core;
using MOUSE.Core.Actors;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.MessageProcessing;
using PerfTests.Protocol;

namespace SFActors.Gateway
{
    public class MouseActorsBackendClientNetChannel : AsyncProcessingNetChannel
    {
        readonly IActorSystem<ITestActor> _actorSystem;
        static readonly Timer BackendCallsTimer = Metric.Context("PublicNet").Timer("BackendCallsTimer", Unit.Calls);

        IActorProxy _actor;
        public MouseActorsBackendClientNetChannel(IActorSystem<ITestActor> actorSystem, INetNode node, ITransportChannel transportChannel, IMessageSerializer serializer, 
            ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
            : base(node, transportChannel, serializer, logger, config, bufferPool)
        {
            _actorSystem = actorSystem;
        }

        protected override IMessageProcessingPipeBuilder Configure(IMessageProcessingPipeBuilder builder)
        {
            return builder
                //.UseIdleDisconnect(TimeSpan.FromSeconds(60 * 10), this)
                //.UseInvalidHandshakeDisconnect<JoinRoomS2S>(TimeSpan.FromSeconds(5))
                .UseFiber(new SimpleFiber(TaskScheduler.Default, 10))
                .UseConfigurableDispatcher(config => config
                    .HandleRequestAsync<UseActor, OperationResult>(Process)
                    .HandleRequestAsync<PingGateway, OperationResult>(Process)
                    .HandleRequestAsync<TestStateless, OperationResult>(Process)
                    .HandleRequestAsync<TestStatelessUnreliable, OperationResult>(Process)
                    .HandleRequestAsync<TestStateful, OperationResult>(Process),

                    throwIfUnhandled: false);
        }

        async Task<OperationResult> Process(TestStatelessUnreliable msg, IOperationContext context)
        {
            using (BackendCallsTimer.NewContext())
                return await _actor.SendAsync<OperationResult>(msg, context);
        }

        async Task<OperationResult> Process(UseActor msg, IOperationContext context)
        {
            _actor = await _actorSystem.GetActor(new ActorKey(msg.ActorName));
            return OperationResult.Success;
        }

        async Task<OperationResult> Process(TestStateless msg, IOperationContext context)
        {
            using (BackendCallsTimer.NewContext())
                return await _actor.SendAsync<OperationResult>(msg, context);
        }

        async Task<OperationResult> Process(PingGateway msg, IOperationContext context)
        {
            using (BackendCallsTimer.NewContext())
            {
                await Task.Delay(msg.SleepDurationMs);
                return OperationResult.Success;
            }
        }

        async Task<OperationResult> Process(TestStateful msg, IOperationContext context)
        {
            using (BackendCallsTimer.NewContext())
                return await _actor.SendAsync<OperationResult>(msg, context);
        }
    }
}