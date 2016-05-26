using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using Autofac;
using Lidgren.Network;
using LidgrenWrap;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using MOUSE.Core;
using MOUSE.Core.Actors;
using MOUSE.Core.Azure.ServiceFabric;
using MOUSE.Core.Configuration;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Logging;
using MOUSE.Core.Misc;
using MOUSE.Core.Serialization;

namespace ActorChat.Services.RoomActorService
{
    internal sealed class RoomActorService : StatefulService
    {
        IEnumerable<INetNode> _netNodes;

        public RoomActorService(StatefulServiceContext context)
            : base(context)
        {
            float timeoutDebugMultiplier = 10;
            var builder = new ContainerBuilder();

            builder.Register(c => new ProtobufMessageSerializer(typeof(Message).Assembly, typeof(JoinRoom).Assembly, typeof(JoinRoomS2S).Assembly)).As<IMessageSerializer>();

            builder.RegisterInstance(CoreEventsETWLogger.Instance).As<ICoreEvents>();
            builder.RegisterInstance(ActorCoreEventsETWLogger.Instance).As<IActorCoreEvents>();
            builder.RegisterInstance(LidgrenEventsETWLogger.Instance).As<ILidgrenEvents>();

            var roomActorsEndpoint = FabricRuntime.GetActivationContext().GetEndpoint("RoomActors");

            var nodeIP = Dns.GetHostAddresses(FabricRuntime.GetNodeContext().IPAddressOrFQDN).First(x => x.AddressFamily == AddressFamily.InterNetwork);

            var roomActorsNetConfig = new NetPeerConfiguration("RoomActors")
            {
                LocalAddress = nodeIP,
                MaximumConnections = 10000,
                AcceptIncomingConnections = true,
                Port = roomActorsEndpoint.Port,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };

            var callbacksNetConfig = new NetPeerConfiguration("Callbacks")
            {
                AcceptIncomingConnections = false,
                ConnectionTimeout = 10 * timeoutDebugMultiplier
            };


            builder.RegisterType<WcfBufferPool>().As<IBufferPool>();

           
            builder.Register(c =>
                new CallbacksGatewayNode("Callbacks",
                    new LidgrenNetProvider(callbacksNetConfig, c.Resolve<ILidgrenEvents>()),
                    c.Resolve<ICoreEvents>(), c.Resolve<IMessageSerializer>(), c.Resolve<INetNodeConfig>(), c.Resolve<IBufferPool>()))
                .As<ICallbacksGatewayNode>()
                .As<INetNode>()
                .SingleInstance();

            builder.Register(c => new NetNodeConfig()
            {
                SendTimeoutSec = (int)(10.0 * timeoutDebugMultiplier),
                ConnectTimeoutSec = (int)(10 * timeoutDebugMultiplier)
            }).As<INetNodeConfig>();

            builder.Register(c => new RoomActor(c.Resolve<ICallbacksGatewayNode>(), StateManager)).As<IRoomActor>();
            builder.Register(c =>
                    new ServiceFabricActorSystemNetNode<IRoomActor>("RoomActors", new Uri("fabric:/ActorChat.ServiceFabric/RoomActorService"),
                        new LidgrenNetProvider(roomActorsNetConfig, c.Resolve<ILidgrenEvents>()),
                        c.Resolve<IActorCoreEvents>(), c.Resolve<ICoreEvents>(), c.Resolve<IMessageSerializer>(), c.Resolve<INetNodeConfig>(), c.Resolve<IBufferPool>(),
                        actorFactory: c.Resolve<Func<IRoomActor>>()))
               .As<INetNode>()
               .Named<INetNode>("RoomActors")
               .As<IActorSystem<IRoomActor>>()
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
    }

}
