using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Logging.Serilog;
using MOUSE.Core.Serialization;
using PerfTests.Protocol;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using global::Serilog;
using Microsoft.Orleans.ServiceFabric.Client;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Communication.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using MOUSE.Core.Interfaces.Actors;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace SFActors.Gateway
{
    public sealed class GatewayService : StatelessService
    {
        readonly IEnumerable<INetNode> _netNodes;
        IDisposable _metricsSubscription;

        public GatewayService(StatelessServiceContext context)
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

            builder.Register(
                c => new ProtobufMessageSerializer(typeof(Message).Assembly, typeof(TestStateless).Assembly))
                .As<IMessageSerializer>();

            var publicEndpoint = FabricRuntime.GetActivationContext().GetEndpoint("Public");
            var public2Endpoint = FabricRuntime.GetActivationContext().GetEndpoint("Public2");
            var public3Endpoint = FabricRuntime.GetActivationContext().GetEndpoint("Public3");
            var nodeIP = Dns.GetHostAddresses(FabricRuntime.GetNodeContext().IPAddressOrFQDN).First(x => x.AddressFamily == AddressFamily.InterNetwork);

            var publicNetConfig = new NetPeerConfiguration("PublicNet")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 10000,
                AcceptIncomingConnections = true,
                Port = publicEndpoint.Port,
                ConnectionTimeout = 10*timeoutDebugMultiplier
            };
            var public2NetConfig = new NetPeerConfiguration("PublicNet")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 10000,
                AcceptIncomingConnections = true,
                Port = public2Endpoint.Port,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            var public3NetConfig = new NetPeerConfiguration("PublicNet")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 10000,
                AcceptIncomingConnections = true,
                Port = public3Endpoint.Port,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            var testActorsNetConfig = new NetPeerConfiguration("TestActors")
            {
                LocalAddress = nodeIP,
                AcceptIncomingConnections = false,
                Port = 0,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            builder.RegisterType<WcfBufferPool>().As<IBufferPool>();

            builder.Register(c =>
                {
                    var messageSerialer = c.Resolve<IMessageSerializer>();
                    var coreLogger = c.Resolve<ICoreEvents>();
                    var nedNodeConfig = c.Resolve<INetNodeConfig>();
                    var bufferPool = c.Resolve<IBufferPool>();

                    return new NetNode<SFActorsBackendClientNetChannel>("PublicNet",
                        new LidgrenNetProvider(publicNetConfig, c.Resolve<ILidgrenEvents>()),
                        coreLogger, messageSerialer,
                        (node, transport) => new SFActorsBackendClientNetChannel(node, transport, messageSerialer, coreLogger, nedNodeConfig, bufferPool),
                        nedNodeConfig);
                })
                .As<INetNode>()
                .SingleInstance();

            builder.Register(c =>
                {
                    var actorSystem = c.Resolve<IActorSystem<ITestActor>>();
                    var messageSerialer = c.Resolve<IMessageSerializer>();
                    var coreLogger = c.Resolve<ICoreEvents>();
                    var netNodeConfig = c.Resolve<INetNodeConfig>();
                    var bufferPool = c.Resolve<IBufferPool>();

                    return new NetNode<MouseActorsBackendClientNetChannel>("PublicNet2",
                        new LidgrenNetProvider(public2NetConfig, c.Resolve<ILidgrenEvents>()),
                        coreLogger, messageSerialer,
                        (node, transport) => new MouseActorsBackendClientNetChannel(actorSystem,  node, transport, messageSerialer, coreLogger, netNodeConfig, bufferPool),
                        netNodeConfig);
                })
                .As<INetNode>()
                .SingleInstance();

            builder.Register(c =>
            {
                var messageSerialer = c.Resolve<IMessageSerializer>();
                var coreLogger = c.Resolve<ICoreEvents>();
                var netNodeConfig = c.Resolve<INetNodeConfig>();
                var bufferPool = c.Resolve<IBufferPool>();

                return new NetNode<OrleansBackendClientNetChannel>("PublicNet3",
                    new LidgrenNetProvider(public3NetConfig, c.Resolve<ILidgrenEvents>()),
                    coreLogger, messageSerialer,
                    (node, transport) => new OrleansBackendClientNetChannel(node, transport, messageSerialer, coreLogger, netNodeConfig, bufferPool),
                    netNodeConfig);
            })
                .As<INetNode>()
                .SingleInstance();

            builder.Register(c =>
                    new ServiceFabricActorSystemNetNode<ITestActor>("TestActors", new Uri("fabric:/MouseTestActor.Deploy/MouseTestActor"),
                        new LidgrenNetProvider(testActorsNetConfig, c.Resolve<ILidgrenEvents>()),
                        c.Resolve<IActorCoreEvents>(), c.Resolve<ICoreEvents>(), c.Resolve<IMessageSerializer>(), c.Resolve<INetNodeConfig>(), c.Resolve<IBufferPool>()))
               .As<INetNode>()
               .As<IActorSystem<ITestActor>>()
               .SingleInstance();

            builder.Register(c => new NetNodeConfig()
            {
                SendTimeoutSec = (int) (10.0*timeoutDebugMultiplier),
                ConnectTimeoutSec = (int) (10*timeoutDebugMultiplier)
            }).As<INetNodeConfig>();

            var container = builder.Build();

            _netNodes = container.Resolve<IEnumerable<INetNode>>();


            var config = new ClientConfiguration
            {
                DataConnectionString = "DefaultEndpointsProtocol=https;AccountName=actorchatstorage;AccountKey=1hCY/Ak2TFrqE61cMhbPU5rkv9PuDfX7QQFU4tXCSc2AO78hLdm6u3PrGrZbUzOj7OkIZ93YKbU81VSVnBMbPg==",
                PropagateActivityId = true,
                DefaultTraceLevel = Severity.Info,
                GatewayProvider = ClientConfiguration.GatewayProviderType.AzureTable,
                TraceToConsole = true,
                StatisticsCollectionLevel = StatisticsLevel.Critical,
                StatisticsLogWriteInterval = TimeSpan.FromDays(6),
                TraceFileName = null,
                TraceFilePattern = null,
                ResponseTimeout = TimeSpan.FromSeconds(90),
                StatisticsMetricsTableWriteInterval = TimeSpan.FromDays(6),
                StatisticsPerfCountersWriteInterval = TimeSpan.FromDays(6),
            };
            OrleansFabricClient.Initialize(new Uri("fabric:/OrleansTest/OrleansTestActor"), config);
            
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            foreach (var node in _netNodes)
            {
                yield return
                    new ServiceInstanceListener(
                        _ => new ServiceFabricCommunicationListenerAdapter(node, transportSchema: "net.udp"),
                        node.InstanceName);
            }

            // we need this because Azure LB doesnt support UDP probes
            yield return new ServiceInstanceListener(context => 
                new FabricTransportServiceRemotingListener(context, 
                    new LoadbalancingProbeReplier(),    
                    new FabricTransportListenerSettings()
                    {
                        EndpointResourceName = "LoadBalancerReplier"
                    }), "LoadBalancerReplier");
        }

        async protected override Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        protected override void OnAbort()
        {
            _metricsSubscription.Dispose();
            base.OnAbort();
        }
    }

    public class LoadbalancingProbeReplier : IServiceRemotingMessageHandler
    {
        public Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody)
        {
            return Task.FromResult(new byte[0]);
        }

        public void HandleOneWay(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody)
        {
        }
    }
}
