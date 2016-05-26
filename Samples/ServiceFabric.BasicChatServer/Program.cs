using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ServiceFabric.BasicChatServer
{
    static public class Program
    {
        static void Main()
        {
            try
            {
                ServiceRuntime.RegisterServiceAsync("BasicChatServerType", context => new BasicChatServer(context)).Wait();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(BasicChatServer).Name);

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
