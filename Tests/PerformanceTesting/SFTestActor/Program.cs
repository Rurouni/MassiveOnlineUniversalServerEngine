using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Core.Telemetry;
using Metrics;
using Microsoft.ServiceFabric.Actors.Runtime;
using MOUSE.Core.Azure.ServiceFabric;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using global::Serilog;
using MOUSE.Core.Logging.Serilog;

namespace SFTestActor
{
    static internal class Program
    {

        static void Main()
        {
            try
            {
                ActorRuntime.RegisterActorAsync<SFTestActor>(
                   (context, actorType) => new TelemetryEnabledActorService(context, actorType, () => new SFTestActor())).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }

    public class TelemetryEnabledActorService : ActorService
    {

        public TelemetryEnabledActorService(StatefulServiceContext context, ActorTypeInformation actorTypeInfo, Func<ActorBase> actorFactory = null, IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) : base(context, actorTypeInfo, actorFactory, stateProvider, settings)
        {}

        async protected override Task RunAsync(CancellationToken cancellationToken)
        {
            var esLogsConfig = new FabricConfigProvider<ElasticSearchOutputConfig>("ElasticSearchLogs").Config;
            var esMetricsConfig = new FabricConfigProvider<ElasticSearchOutputConfig>("ElasticSearchMetrics").Config;

            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Warning()
                .Enrich.With(new AzureServiceFabricSerilogEnricher(Context))
                .Enrich.With<ExceptionEnricher>()
                .WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri(esLogsConfig.ElasticSearchUri))
                    {
                        IndexFormat = $"{esLogsConfig.ElasticSearchIndexName}-{{0:yyyy.MM.dd}}"
                    })
                .CreateLogger();
            Log.Logger = logger;


            Metric.Config.WithAllCounters();
            //Metric.PerformanceCounter("Actor calls waiting for actor lock", "Service Fabric Actor", "# of actor calls waiting for actor lock", "*", Unit.Items);
            //Metric.PerformanceCounter("Actor outstanding requests", "Service Fabric Actor", "# of outstanding requests", "*", Unit.Items);
            //Metric.PerformanceCounter("Actor Average milliseconds actor lock held", "Service Fabric Actor", "Average milliseconds actor lock held", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Average milliseconds for request deserialization", "Service Fabric Actor", "Average milliseconds for request deserialization", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Average milliseconds for response serialization", "Service Fabric Actor", "Average milliseconds for response serialization", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Average milliseconds per load state operation", "Service Fabric Actor", "Average milliseconds per load state operation", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Average milliseconds per lock wait", "Service Fabric Actor", "Average milliseconds per lock wait", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Average milliseconds per request", "Service Fabric Actor", "Average milliseconds per request", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Average milliseconds per save state operation", "Service Fabric Actor", "Average milliseconds per save state operation", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Average OnActivateAsync milliseconds", "Service Fabric Actor", "Average OnActivateAsync milliseconds", "*", Unit.Custom("ms"));

            //Metric.PerformanceCounter("Actor Method Average milliseconds per invocation", "Service Fabric Actor Method", "Average milliseconds per invocation", "*", Unit.Custom("ms"));
            //Metric.PerformanceCounter("Actor Method Exceptions thrown/Sec", "Service Fabric Actor Method", "Exceptions thrown/Sec", "*", Unit.Custom("rate/sec"));
            //Metric.PerformanceCounter("Actor Method Invocations/Sec", "Service Fabric Actor Method", "Invocations/Sec", "*", Unit.Custom("rate/sec"));


            var metricsSubscription = new TelemetryPipe()
                .CollectMetricsNet(5, ServiceFabricHelpers.GetEnvironmentProperties(Context), true)
                .SendToElasticSearch(esMetricsConfig)
                .Start();

            var logsSubscription = new TelemetryPipe()
                .CollectEventSourceEvents(new[]
                    {
                        //new ETWProvider("Microsoft-ServiceFabric-Services"),
                        //new ETWProvider("Microsoft-ServiceFabric-Actors"),
                        new ETWProvider("SFActorsPerfTest-SFTestActor")
                    }, ServiceFabricHelpers.GetEnvironmentProperties(Context))
                .SendToElasticSearch(esLogsConfig)
                .Start();


            using (logsSubscription)
            using (metricsSubscription)
            {
                await base.RunAsync(cancellationToken);

                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
    }
}
