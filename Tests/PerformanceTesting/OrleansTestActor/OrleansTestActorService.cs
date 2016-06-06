using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Telemetry;
using Core.Telemetry.Orleans.MetricsNet;
using Core.Telemetry.Orleans.Serilog;
using Metrics;
using Microsoft.Orleans.ServiceFabric.Silo;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using MOUSE.Core.Azure.ServiceFabric;
using MOUSE.Core.Logging.Serilog;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Counters;
using Orleans.Storage;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

namespace OrleansTestActor
{
    internal sealed class OrleansTestActorService : StatelessService
    {
	    private IDisposable _disposable;

	    public OrleansTestActorService(StatelessServiceContext context)
		    : base(context)
	    {
		    var esLogsConfig = new FabricConfigProvider<ElasticSearchOutputConfig>("ElasticSearchLogs").Config;
			var esMetricsConfig = new FabricConfigProvider<ElasticSearchOutputConfig>("ElasticSearchMetrics").Config;

			var logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.With(new AzureServiceFabricSerilogEnricher(context))
				.Enrich.With<ExceptionEnricher>()
				.Enrich.With<ActivityIdSerilogEnricher>()
				.WriteTo.Elasticsearch(
					new ElasticsearchSinkOptions(new Uri(esLogsConfig.ElasticSearchUri))
					{
						IndexFormat = $"{esLogsConfig.ElasticSearchIndexName}-{{0:yyyy.MM.dd}}"
					})
				.CreateLogger();
			Log.Logger = logger;

			
			//Metric.Config.WithAllCounters();

			_disposable = new TelemetryPipe()
				.CollectMetricsNet(10, ServiceFabricHelpers.GetEnvironmentProperties(context), true)
				.SendToElasticSearch(esMetricsConfig)
				.Start();

			TraceLogger.BulkMessageInterval = TimeSpan.FromSeconds(10);
			Logger.TelemetryConsumers.Add(new SerilogTelemetryConsumer());
			Logger.TelemetryConsumers.Add(new MetricNetTelemetryConsumer());
		}

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
			var silo =
                    new ServiceInstanceListener(
                        parameters =>
                        new OrleansCommunicationListener(parameters, GetClusterConfiguration(), Partition));
            return new[] { silo };
        }

        protected async override Task RunAsync(CancellationToken cancellationToken)
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
            config.Defaults.StatisticsLogWriteInterval = TimeSpan.FromSeconds(10);
            config.Defaults.TurnWarningLengthThreshold = TimeSpan.FromSeconds(15);
            config.Defaults.TraceToConsole = true;
            config.Defaults.DefaultTraceLevel = Severity.Info;

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
	        config.Defaults.PropagateActivityId = true;
			
            return config;
        }
    }

    internal class OrleansConfig
    {
        public string DataConnectionString { get; set; }
    }
}
