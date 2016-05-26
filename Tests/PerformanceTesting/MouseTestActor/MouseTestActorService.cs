using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Core.Telemetry;
using Lidgren.Network;
using LidgrenWrap;
using Metrics;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using MOUSE.Core;
using MOUSE.Core.Azure.ServiceFabric;
using MOUSE.Core.Configuration;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Logging;
using MOUSE.Core.Logging.Serilog;
using MOUSE.Core.Serialization;
using PerfTests.Protocol;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

namespace MouseTestActor
{
    internal sealed class MouseTestActorService : StatefulService
    {
        IEnumerable<INetNode> _netNodes;
        IDisposable _metricsSubscription;

        public MouseTestActorService(StatefulServiceContext context)
            : base(context)
        {
            WinApi.TimeBeginPeriod(1); //improve sleep precision for polling type transports

            float timeoutDebugMultiplier = 1;
            var builder = new ContainerBuilder();

            var esLogsConfig = new FabricConfigProvider<ElasticSearchOutputConfig>("ElasticSearchLogs").Config;
            var esMetricsConfig = new FabricConfigProvider<ElasticSearchOutputConfig>("ElasticSearchMetrics").Config;

            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Error()
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

            builder.RegisterInstance(logger).As<ILogger>();
            builder.RegisterType<SerilogCoreEvents>().As<ICoreEvents>();
            builder.RegisterType<SerilogActorCoreEvents>().As<IActorCoreEvents>();
            builder.RegisterType<SerilogLidgrenEvents>().As<ILidgrenEvents>();

            Metric.Config.WithAllCounters();

            _metricsSubscription = new TelemetryPipe()
                .CollectMetricsNet(5, ServiceFabricHelpers.GetEnvironmentProperties(context), true)
                .SendToElasticSearch(esMetricsConfig)
                .Start();

            builder.Register(c => new ProtobufMessageSerializer(typeof(Message).Assembly, typeof(TestStateless).Assembly)).As<IMessageSerializer>();

            var roomActorsEndpoint = FabricRuntime.GetActivationContext().GetEndpoint("TestActors");

            var nodeIP = Dns.GetHostAddresses(FabricRuntime.GetNodeContext().IPAddressOrFQDN).First(x => x.AddressFamily == AddressFamily.InterNetwork);

            var roomActorsNetConfig = new NetPeerConfiguration("TestActors")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 10000,
                AcceptIncomingConnections = true,
                Port = roomActorsEndpoint.Port,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };


            builder.RegisterType<WcfBufferPool>().As<IBufferPool>();

            builder.Register(c => new NetNodeConfig()
            {
                SendTimeoutSec = (int)(10.0 * timeoutDebugMultiplier),
                ConnectTimeoutSec = (int)(10 * timeoutDebugMultiplier)
            }).As<INetNodeConfig>();

            builder.Register(c => new TestActor(StateManager)).As<ITestActor>();
            builder.Register(c =>
                    new ServiceFabricActorSystemNetNode<ITestActor>("TestActors", Context.ServiceName,
                        new LidgrenNetProvider(roomActorsNetConfig, c.Resolve<ILidgrenEvents>()),
                        c.Resolve<IActorCoreEvents>(), c.Resolve<ICoreEvents>(), c.Resolve<IMessageSerializer>(), c.Resolve<INetNodeConfig>(), c.Resolve<IBufferPool>(),
                        actorFactory: c.Resolve<Func<ITestActor>>()))
               .As<INetNode>()
               .SingleInstance();

            var container = builder.Build();


            _netNodes = container.Resolve<IEnumerable<INetNode>>();
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            foreach (var node in _netNodes)
            {
                yield return new ServiceReplicaListener(_ => new ServiceFabricCommunicationListenerAdapter(node, transportSchema: "net.udp"), node.InstanceName);
            }
        }

        async protected override Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        protected override void OnAbort()
        {
            _metricsSubscription.Dispose();
            base.OnAbort();
        }
    }
}
