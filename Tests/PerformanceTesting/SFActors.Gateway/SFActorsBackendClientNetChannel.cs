using System;
using System.Threading.Tasks;
using Metrics;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using MOUSE.Core;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.MessageProcessing;
using PerfTests.Protocol;
using SFTestActor.Interfaces;

namespace SFActors.Gateway
{
    public class SFActorsBackendClientNetChannel : AsyncProcessingNetChannel
    {
        static readonly Timer BackendCallsTimer = Metric.Context("PublicNet").Timer("BackendCallsTimer", Unit.Calls);

        ISFTestActor _actor;
        public SFActorsBackendClientNetChannel(INetNode node, ITransportChannel transportChannel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
            : base(node, transportChannel, serializer, logger, config, bufferPool)
        {
        }

        protected override IMessageProcessingPipeBuilder Configure(IMessageProcessingPipeBuilder builder)
        {
            return builder
                //.UseIdleDisconnect(TimeSpan.FromSeconds(60 * 10), this)
                //.UseInvalidHandshakeDisconnect<JoinRoomS2S>(TimeSpan.FromSeconds(5))
                .UseFiber(new SimpleFiber(TaskScheduler.Default, 10))
                .UseConfigurableDispatcher(config => config
                    .HandleRequest<UseActor, OperationResult>(OnUseActor)
                    .HandleRequestAsync<PingGateway, OperationResult>(OnPingGateway)
                    .HandleRequestAsync<TestStateless, OperationResult>(OnTestStateless)
                    .HandleRequestAsync<TestStateful, OperationResult>(OnTestStateful),

                    throwIfUnhandled: false);
        }

        OperationResult OnUseActor(UseActor msg, IOperationContext context)
        {
            _actor = ActorProxy.Create<ISFTestActor>(new ActorId(msg.ActorName), new Uri("fabric:/SFActorsPerfTest/SFTestActorService"));
            return OperationResult.Success;
        }

        async Task<OperationResult> OnTestStateless(TestStateless msg, IOperationContext context)
        {
            using (BackendCallsTimer.NewContext())
                return await _actor.TestStateless(msg);
        }

        async Task<OperationResult> OnPingGateway(PingGateway msg, IOperationContext context)
        {
            using (BackendCallsTimer.NewContext())
            {
                await Task.Delay(msg.SleepDurationMs);
                return OperationResult.Success;
            }
        }

        async Task<OperationResult> OnTestStateful(TestStateful msg, IOperationContext context)
        {
            using (BackendCallsTimer.NewContext())
                return await _actor.TestStateful(msg);
        }
    }
}