using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using MOUSE.Core;
using PerfTests.Protocol;
using SFTestActor.Interfaces;

namespace SFTestActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class SFTestActor : Actor, ISFTestActor
    {
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            return this.StateManager.TryAddStateAsync("count", 0);
        }

        Task<int> ISFTestActor.GetCountAsync()
        {
            return this.StateManager.GetStateAsync<int>("count");
        }

        Task ISFTestActor.SetCountAsync(int count)
        {
            return this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value);
        }

        async public Task<OperationResult> TestStateless(TestStateless msg)
        {
            if (msg.SleepDurationMs > 0)
                await Task.Delay(msg.SleepDurationMs);

            return OperationResult.Success;
        }

        async public Task<OperationResult> TestStateful(TestStateful msg)
        {
            await StateManager.AddOrUpdateStateAsync("count", 1, (key, value) => value+1);
            return OperationResult.Success;
        }
    }
}
