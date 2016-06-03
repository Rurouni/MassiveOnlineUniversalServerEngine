using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using Autofac;
using Lidgren.Network;
using LidgrenWrap;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using MOUSE.Core;
using MOUSE.Core.Azure;
using MOUSE.Core.Configuration;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Logging;
using MOUSE.Core.Logging.Serilog;
using MOUSE.Core.Serialization;
using Serilog;

namespace ActorChat.Services.EventHubRoomActorService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class EventHubRoomActorService : StatelessService
    {
        public EventHubRoomActorService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        async protected override Task RunAsync(CancellationToken cancellationToken)
        {
            float timeoutDebugMultiplier = 10;
            var builder = new ContainerBuilder();

            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Verbose()
                .Enrich.With(new AzureServiceFabricSerilogEnricher(Context))
                .WriteTo.Trace()
                .CreateLogger();

            builder.RegisterInstance(logger).As<ILogger>();

            builder.RegisterType<SerilogCoreEvents>().As<ICoreEvents>();
            builder.RegisterType<SerilogActorCoreEvents>().As<IActorCoreEvents>();
            builder.RegisterType<SerilogLidgrenEvents>().As<ILidgrenEvents>();
            builder.RegisterType<SerilogAzureEventHubActorSystemEvents>().As<IAzureEventHubActorSystemEvents>();

            builder.Register(c => new ProtobufMessageSerializer(typeof(Message).Assembly, typeof(JoinRoom).Assembly, typeof(JoinRoomS2S).Assembly))
                .As<IMessageSerializer>();
           
            var internalNetConfig = new NetPeerConfiguration("Callbacks")
            {
                LocalAddress = IPAddress.Any,
                MaximumConnections = 1000,
                AcceptIncomingConnections = false,
                Port = 0,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            builder.Register<Func<INetNode, ITransportChannel, NetChannel>>(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return
                    (node, transport) =>
                        new NetChannel(node, transport, ctx.Resolve<IMessageSerializer>(), ctx.Resolve<ICoreEvents>(), ctx.Resolve<INetNodeConfig>(),
                            ctx.Resolve<IBufferPool>());
            })
                .As<Func<INetNode, ITransportChannel, NetChannel>>();


            builder.RegisterType<WcfBufferPool>().As<IBufferPool>();

            builder.Register(c =>
                new CallbacksGatewayNode("Callbacks",
                    new LidgrenNetProvider(internalNetConfig, c.Resolve<ILidgrenEvents>()),
                    c.Resolve<ICoreEvents>(), c.Resolve<IMessageSerializer>(), c.Resolve<INetNodeConfig>(), c.Resolve<IBufferPool>()))
                .As<ICallbacksGatewayNode>()
                .SingleInstance();

            builder.Register(
                c => new NetNodeConfig() { SendTimeoutSec = (int)(30.0 * timeoutDebugMultiplier), ConnectTimeoutSec = (int)(30 * timeoutDebugMultiplier) })
                .As<INetNodeConfig>();

            builder.RegisterType<RoomActor>()
                .AsSelf();

            var eventHubConnectionString =
                @"Endpoint=sb://rurouni.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=rNti8MnSE2Kp4EVpjoBsk1WgvWwHb4nSDcmADmAvWKM=";
            var blobstoreConnectionName =
                @"DefaultEndpointsProtocol=https;AccountName=actorchatstorage;AccountKey=1hCY/Ak2TFrqE61cMhbPU5rkv9PuDfX7QQFU4tXCSc2AO78hLdm6u3PrGrZbUzOj7OkIZ93YKbU81VSVnBMbPg==;BlobEndpoint=https://actorchatstorage.blob.core.windows.net/;TableEndpoint=https://actorchatstorage.table.core.windows.net/;QueueEndpoint=https://actorchatstorage.queue.core.windows.net/;FileEndpoint=https://actorchatstorage.file.core.windows.net/";

            builder
                .Register(c =>
                    new AzureEventHubActorSystem<RoomActor>(1, "test2", eventHubConnectionString, blobstoreConnectionName, true,
                        c.Resolve<Func<RoomActor>>(), c.Resolve<INetNodeConfig>(), c.Resolve<ICallbacksGatewayNode>(), c.Resolve<IMessageSerializer>(),
                        c.Resolve<IAzureEventHubActorSystemEvents>(), TimeSpan.FromSeconds(10)))
                .As<IActorSystem<RoomActor>>()
                .SingleInstance();

            var container = builder.Build();

            var internalNode = container.Resolve<ICallbacksGatewayNode>();
            internalNode.Start();

            var roomActors = container.Resolve<IActorSystem<RoomActor>>();
            roomActors.Start();

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
