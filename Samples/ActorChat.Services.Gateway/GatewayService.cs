using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using ActorChat.Services.EventHubRoomActorService;
using Autofac;
using Core.Azure.Web;
using Lidgren.Network;
using LidgrenWrap;
using Metrics;
using Metrics.Logging;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using MOUSE.Core;
using MOUSE.Core.Azure;
using MOUSE.Core.Azure.ServiceFabric;
using MOUSE.Core.Configuration;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Logging;
using MOUSE.Core.Logging.Serilog;
using MOUSE.Core.Serialization;
using Owin;
using Owin.Metrics;
using Serilog;
using Serilog.Exceptions;

namespace ActorChat.Services.Gateway
{
    public sealed class GatewayService : StatelessService
    {
        IEnumerable<INetNode> _netNodes;

        public GatewayService(StatelessServiceContext context)
            : base(context)
        {
            float timeoutDebugMultiplier = 10;
            var builder = new ContainerBuilder();

            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Verbose()
                .Enrich.With(new AzureServiceFabricSerilogEnricher(Context))
                .Enrich.With<ExceptionEnricher>()
                .WriteTo.Trace()
                .CreateLogger();

            builder.RegisterInstance(logger).As<ILogger>();

            builder.Register(c => new ProtobufMessageSerializer(typeof(Message).Assembly, typeof(JoinRoom).Assembly, typeof(JoinRoomS2S).Assembly)).As<IMessageSerializer>();


            builder.RegisterType<SerilogCoreEvents>().As<ICoreEvents>();
            builder.RegisterType<SerilogActorCoreEvents>().As<IActorCoreEvents>();
            builder.RegisterType<SerilogLidgrenEvents>().As<ILidgrenEvents>();
            builder.RegisterType<SerilogAzureEventHubActorSystemEvents>().As<IAzureEventHubActorSystemEvents>();

            var publicEndpoint = FabricRuntime.GetActivationContext().GetEndpoint("Public");
            var publicSFEndpoint = FabricRuntime.GetActivationContext().GetEndpoint("PublicSF");
            var callbacksEndpoint = FabricRuntime.GetActivationContext().GetEndpoint("Callbacks");
            var nodeIP = Dns.GetHostAddresses(FabricRuntime.GetNodeContext().IPAddressOrFQDN).First(x => x.AddressFamily == AddressFamily.InterNetwork);

            var clientFacingNetConfig = new NetPeerConfiguration("Public")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 1000,
                AcceptIncomingConnections = true,
                Port = publicEndpoint.Port,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            var clientFacingSFNetConfig = new NetPeerConfiguration("Public")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 1000,
                AcceptIncomingConnections = true,
                Port = publicSFEndpoint.Port,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            var roomActorsNetConfig = new NetPeerConfiguration("RoomActors")
            {
                LocalAddress = nodeIP,
                AcceptIncomingConnections = false,
                Port = 0,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            var callbacksNetConfig = new NetPeerConfiguration("Callbacks")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 1000,
                AcceptIncomingConnections = true,
                Port = callbacksEndpoint.Port,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };


            builder.RegisterType<WcfBufferPool>().As<IBufferPool>();

            // builder.Register<Func<ITransportChannel, SFChatClientNetChannel>>(c =>
            // {
            //     var ctx = c.Resolve<IComponentContext>();
            //     return
            //         transport => new SFChatClientNetChannel(transport, ctx.Resolve<IMessageSerializer>(),
            //                             ctx.Resolve<ICoreEvents>(), ctx.Resolve<INetNodeConfig>(), ctx.Resolve<IBufferPool>());
            // })
            //.As<Func<ITransportChannel, SFChatClientNetChannel>>();

            builder.Register(c =>
                {
                    var actorSystem = c.Resolve<IActorSystem<IRoomActor>>();
                    var callbacksNode = c.Resolve<ICallbacksGatewayNode>();
                    var messageSerialer = c.Resolve<IMessageSerializer>();
                    var coreLogger = c.Resolve<ICoreEvents>();
                    var nedNodeConfig = c.Resolve<INetNodeConfig>();
                    var bufferPool = c.Resolve<IBufferPool>();

                    return new NetNode<ChatClientNetChannel>("Public",
                        new LidgrenNetProvider(clientFacingNetConfig, c.Resolve<ILidgrenEvents>()),
                        coreLogger, messageSerialer,
                        (node, transport) => new ChatClientNetChannel(actorSystem, callbacksNode, node, transport, messageSerialer, coreLogger, nedNodeConfig, bufferPool),
                        nedNodeConfig);
                })
                .As<INetNode>()
                .AsSelf()
                .SingleInstance();

            builder.Register(c =>
            {
                var messageSerialer = c.Resolve<IMessageSerializer>();
                var coreLogger = c.Resolve<ICoreEvents>();
                var nedNodeConfig = c.Resolve<INetNodeConfig>();
                var bufferPool = c.Resolve<IBufferPool>();

                return new NetNode<SFChatClientNetChannel>("PublicSF",
                    new LidgrenNetProvider(clientFacingSFNetConfig, c.Resolve<ILidgrenEvents>()),
                    coreLogger, messageSerialer,
                    (node, transport) => new SFChatClientNetChannel(node, transport, messageSerialer, coreLogger, nedNodeConfig, bufferPool), 
                    nedNodeConfig);
            })
                .As<INetNode>()
                .AsSelf()
                .SingleInstance();

            builder.Register(c => 
                    new ServiceFabricActorSystemNetNode<IRoomActor>("RoomActors", new Uri("fabric:/ActorChat.ServiceFabric/RoomActorService"),
                        new LidgrenNetProvider(roomActorsNetConfig, c.Resolve<ILidgrenEvents>()),
                        c.Resolve<IActorCoreEvents>(), c.Resolve<ICoreEvents>(), c.Resolve<IMessageSerializer>(), c.Resolve<INetNodeConfig>(), c.Resolve<IBufferPool>()))
               .As<INetNode>()
               .Named<INetNode>("RoomActors")
               .As<IActorSystem<IRoomActor>>()
               .SingleInstance();

            builder.Register(c =>
                    new CallbacksGatewayNode("Callbacks",
                        new LidgrenNetProvider(callbacksNetConfig, c.Resolve<ILidgrenEvents>()),
                        c.Resolve<ICoreEvents>(), c.Resolve<IMessageSerializer>(), c.Resolve<INetNodeConfig>(), c.Resolve<IBufferPool>(),
                        new IPEndPoint(nodeIP, callbacksEndpoint.Port)))
                .As<ICallbacksGatewayNode>()
                .As<INetNode>()
                .SingleInstance();

            builder.Register(c => new NetNodeConfig()
            {
                SendTimeoutSec = (int)(10.0 * timeoutDebugMultiplier),
                ConnectTimeoutSec = (int)(10 * timeoutDebugMultiplier)
            }).As<INetNodeConfig>();

            //var eventHubConnectionString = @"Endpoint=sb://rurouni.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=rNti8MnSE2Kp4EVpjoBsk1WgvWwHb4nSDcmADmAvWKM=";
            //var blobstoreConnectionName = @"DefaultEndpointsProtocol=https;AccountName=actorchatstorage;AccountKey=1hCY/Ak2TFrqE61cMhbPU5rkv9PuDfX7QQFU4tXCSc2AO78hLdm6u3PrGrZbUzOj7OkIZ93YKbU81VSVnBMbPg==;BlobEndpoint=https://actorchatstorage.blob.core.windows.net/;TableEndpoint=https://actorchatstorage.table.core.windows.net/;QueueEndpoint=https://actorchatstorage.queue.core.windows.net/;FileEndpoint=https://actorchatstorage.file.core.windows.net/";

            //builder
            //    .Register(c =>
            //        new AzureEventHubActorSystem<IRoomActor>("test2", eventHubConnectionString, blobstoreConnectionName, false,
            //            c.Resolve<Func<IRoomActor>>(), c.Resolve<INetNodeConfig>(), c.Resolve<ICallbacksGatewayNode>(), c.Resolve<IMessageSerializer>(),
            //            c.Resolve<IAzureEventHubActorSystemEvents>(), TimeSpan.FromSeconds(10)))
            //    .As<IActorSystem<IRoomActor>>()
            //    .SingleInstance();


            var container = builder.Build();

            //var roomActors = container.Resolve<IActorSystem<IRoomActor>>();
            //roomActors.Start();

            _netNodes = container.Resolve<IEnumerable<INetNode>>();
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            foreach (var node in _netNodes)
            {
                yield return new ServiceInstanceListener(_ => new ServiceFabricCommunicationListenerAdapter(node, transportSchema:"net.udp"), node.InstanceName);
            }

            yield return new ServiceInstanceListener(x => new OwinCommunicationListener(Startup.ConfigureApp, x, "Metrics"), "Metrics");
        }

        async protected override Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    static public class Startup
    {
        static public void ConfigureApp(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //config.MessageHandlers.Add(new SetOwinRouteTemplateMessageHandler());

            string root = ConfigurationManager.AppSettings["Metrics.GlobalContextName"];
            ConfigurationManager.AppSettings["Metrics.GlobalContextName"] = root + "." + Environment.MachineName;

            Metric.Config
                .WithAllCounters()
                .WithOwin(middleware => appBuilder.Use(middleware), c => c
                    .WithRequestMetricsConfig(mc => mc.WithAllOwinMetrics())
                    .WithMetricsEndpoint()
                );

            appBuilder.UseWebApi(config);
        }
    }

    
}
