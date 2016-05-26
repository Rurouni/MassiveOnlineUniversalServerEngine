using System;
using System.Diagnostics;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ActorChat.Services.Gateway
{
    internal static class Program
    {
        static void Main()
        {
            try
            {
                ServicePointManager.DefaultConnectionLimit = 1000;
                ThreadPool.SetMinThreads(10, 10);

                ServiceRuntime.RegisterServiceAsync("GatewayServiceType", context => new GatewayService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(GatewayService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
