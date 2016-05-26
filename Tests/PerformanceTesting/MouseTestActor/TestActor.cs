using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using MOUSE.Core;
using MOUSE.Core.Actors;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.MessageProcessing;
using PerfTests.Protocol;

namespace MouseTestActor
{
    public class TestActor : Actor, ITestActor
    {
        readonly IReliableStateManager _stateManager;
        IReliableDictionary<string, DateTime> _state;

        public TestActor(IReliableStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        protected override IMessageProcessingLockConfigBuilder ConfigureLocks(IMessageProcessingLockConfigBuilder builder)
        {
            return base.ConfigureLocks(builder)
                .Lock<TestStateless>(LockType.Write)
                .Lock<TestStatelessUnreliable>(LockType.Write);
        }

        protected override IMessageHandlingConfigBuilder ConfigureHandlers(IMessageHandlingConfigBuilder builder)
        {
            return builder
                .HandleRequestAsync<TestStateless, OperationResult>(Process)
                .HandleRequestAsync<TestStatelessUnreliable, OperationResult>(Process)
                .HandleRequestAsync<TestStateful, OperationResult>(Process);
        }

        async Task<OperationResult> Process(TestStateless msg, IOperationContext arg2)
        {
            if (msg.SleepDurationMs > 0)
                await Task.Delay(msg.SleepDurationMs);

            return OperationResult.Success;
        }

        async Task<OperationResult> Process(TestStatelessUnreliable msg, IOperationContext arg2)
        {
            if (msg.SleepDurationMs > 0)
                await Task.Delay(msg.SleepDurationMs);

            return OperationResult.Success;
        }

        async Task<OperationResult> Process(TestStateful msg, IOperationContext arg2)
        {
            using (var trans = _stateManager.CreateTransaction())
            {
                _state = await _stateManager.GetOrAddAsync<IReliableDictionary<string, DateTime>>(ActorRef.Key.Id);
                await _state.AddOrUpdateAsync(trans, ActorRef.Key.Id, (x) => DateTime.UtcNow, (x, _) => DateTime.UtcNow);

                await trans.CommitAsync();
            }

            return OperationResult.Success;
        }
    }
}