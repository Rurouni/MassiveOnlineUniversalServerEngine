using System;
using System.Diagnostics;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace ActorChat.Services.SFRoomActor
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                ServicePointManager.DefaultConnectionLimit = 1000;
                ThreadPool.SetMinThreads(10, 10);

                ActorRuntime.RegisterActorAsync<SFRoomActor>(
                   (context, actorType) => new ActorService(context, actorType, () => new SFRoomActor())).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
