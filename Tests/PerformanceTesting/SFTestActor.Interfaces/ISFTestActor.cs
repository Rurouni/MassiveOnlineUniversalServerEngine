using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using MOUSE.Core;
using PerfTests.Protocol;

namespace SFTestActor.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface ISFTestActor : IActor
    {
        Task<OperationResult> TestStateless(TestStateless msg);
        Task<OperationResult> TestStateful(TestStateful msg);
    }
}
