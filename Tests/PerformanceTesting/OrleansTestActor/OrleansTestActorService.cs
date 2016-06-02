using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Telemetry;
using Microsoft.Orleans.ServiceFabric.Silo;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using MOUSE.Core.Azure.ServiceFabric;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Counters;
using Orleans.Storage;

namespace OrleansTestActor
{
    internal sealed class OrleansTestActorService : StatelessService
    {
        public OrleansTestActorService(StatelessServiceContext context)
            : base(context)
        { }
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            var silo =
                    new ServiceInstanceListener(
                        parameters =>
                        new OrleansCommunicationListener(parameters, GetClusterConfiguration(), Partition));
            return new[] { silo };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public ClusterConfiguration GetClusterConfiguration()
        {
            var config = new ClusterConfiguration();

            var orleansConfig = new FabricConfigProvider<OrleansConfig>("OrleansConfig").Config;

            // Configure logging and metrics collection.
            //config.Defaults.StartupTypeName = typeof(SiloServiceLocator).AssemblyQualifiedName;
            config.Defaults.TraceFileName = null;
            config.Defaults.TraceFilePattern = null;
            config.Defaults.StatisticsCollectionLevel = StatisticsLevel.Info;
            config.Defaults.StatisticsLogWriteInterval = TimeSpan.FromSeconds(60);
            config.Defaults.TurnWarningLengthThreshold = TimeSpan.FromSeconds(15);
            config.Defaults.TraceToConsole = true;
            config.Defaults.DefaultTraceLevel = Severity.Warning;

            // Configure providers
            //config.Globals.RegisterStorageProvider<AzureTableStorage>(
            //    "Default",
            //    new Dictionary<string, string>
            //    {
            //        { "DataConnectionString", "DefaultEndpointsProtocol=https;AccountName=actorchatstorage;AccountKey=1hCY/Ak2TFrqE61cMhbPU5rkv9PuDfX7QQFU4tXCSc2AO78hLdm6u3PrGrZbUzOj7OkIZ93YKbU81VSVnBMbPg==" },
            //        { "UseJsonFormat", true.ToString(CultureInfo.InvariantCulture) }
            //    });
            config.Globals.DataConnectionString = orleansConfig.DataConnectionString;
            config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.AzureTable;
            config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.AzureTable;

            //config.Globals.ExpectedClusterSize = nodeList.Count; // An overestimate is tolerable.
            config.Globals.ResponseTimeout = TimeSpan.FromSeconds(30);
            

            return config;
        }
    }

    internal class OrleansConfig
    {
        public string DataConnectionString { get; set; }
    }
}
