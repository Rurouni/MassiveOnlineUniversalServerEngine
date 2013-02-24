using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Isis;
using Lidgren.Network;
using LidgrenWrap;
using MOUSE.Core;
using MOUSE.Core.Actors;
using MOUSE.Core.NodeCoordination;
using NLog;
using Protocol.Generated;
using SampleServer;

namespace SampleServerConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger Log = LogManager.GetLogger("ConsoleHost");


            Log.Info("Starting ISIS");
            var coordinator = new IsisNodeCoordinator();

            IPEndPoint externalEndpoint = null;
            IPEndPoint internalEndpoint = null;
           
            try
            {
                if (args.Length == 2)
                {
                    string[] addrPort = args[0].Split(':');
                    externalEndpoint = new IPEndPoint(IPAddress.Parse(addrPort[0]), int.Parse(addrPort[1]));
                    addrPort = args[1].Split(':');
                    internalEndpoint = new IPEndPoint(IPAddress.Parse(addrPort[0]), int.Parse(addrPort[1]));
                }
                else
                {
                    externalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5055);
                    internalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5056);
                }
            }
            catch (Exception)
            {
                Log.Error("incorrect cmd params - expected:  externalIp:port internalIp:port");
            }
                    
            var builder = new ContainerBuilder();

            //register core messages
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(EmptyMessage)))
                .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
                .As<Message>();

            //register domain messages
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
                .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
                .As<Message>();

            //register domain service definitions and proxies
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
                .Where(x => x.IsAssignableTo<NetProxy>() && x != typeof(NetProxy))
                .As<NetProxy>();

            //register domain service implementations
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(UserManager)))
                .Where(x => x.IsAssignableTo<Actor>() && x != typeof(Actor))
                .As<Actor>();

            builder.RegisterType<OperationDispatcher>().As<IOperationDispatcher>().SingleInstance();
            builder.RegisterType<ActorRepository>().As<IActorRepository>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();

            var externalNetConf = new NetPeerConfiguration("ChatApp")
                {
                    ConnectionTimeout = 30,
                    Port = externalEndpoint.Port,
                    LocalAddress = externalEndpoint.Address
                };
            var internalNetConf = new NetPeerConfiguration("ChatApp")
            {
                ConnectionTimeout = 30,
                Port = internalEndpoint.Port,
                LocalAddress = internalEndpoint.Address
            };
            
            builder.Register(c => new ServerNode(
                    new LidgrenNetProvider(externalNetConf),
                    new LidgrenNetProvider(internalNetConf),
                    coordinator,
                    c.Resolve<IMessageFactory>(), c.Resolve<IOperationDispatcher>(), c.Resolve<IActorRepository>(), () => new ChatClient()))
                .As<IServerNode>().SingleInstance();

            var container = builder.Build();

            var node = container.Resolve<IServerNode>();
            node.Start();

            IsisSystem.WaitForever();
        }
    }
}
