using System;
using System.Threading.Tasks;
using MOUSE.Core;
using Orleans;
using OrleansTestActor.Interface;
using PerfTests.Protocol;

namespace OrleansTestActor
{
    internal class OrleansTestActor : Grain, ITestGrain
    {
        async public Task<OperationResult> TestStateless(TestStateless msg)
        {
            if (msg.SleepDurationMs > 0)
                await Task.Delay(msg.SleepDurationMs);

            return OperationResult.Success;
        }

        async public Task<OperationResult> TestStateful(TestStateful msg)
        {
            throw new NotImplementedException();
        }
    }
}