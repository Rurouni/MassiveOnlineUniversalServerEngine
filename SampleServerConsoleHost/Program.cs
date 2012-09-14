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
using MOUSE.Core;
using NLog;
using Protocol.Generated;
using RakNetWrapper;
using SampleServer;

namespace SampleServerConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger Log = LogManager.GetLogger("ConsoleHost");
            Log.Info("Starting");
            IPEndPoint externalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5055);
            IPEndPoint internalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6055);
            try
            {
                if (args.Length == 2)
                {
                    string[] addrPort = args[0].Split(':');
                    externalEndpoint = new IPEndPoint(IPAddress.Parse(addrPort[0]), int.Parse(addrPort[1]));
                    addrPort = args[1].Split(':');
                    internalEndpoint = new IPEndPoint(IPAddress.Parse(addrPort[0]), int.Parse(addrPort[1]));
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
                .Where(x => x.IsAssignableTo<NodeServiceProxy>() && x != typeof(NodeServiceProxy))
                .As<NodeServiceProxy>();

            //register domain service implementations
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(ChatManager)))
                .Where(x => x.IsAssignableTo<NodeService>() && x != typeof(NodeService))
                .As<NodeService>();

            builder.RegisterType<ChatClient>().As<C2SPeer>();

            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
            builder.RegisterType<ServicesRepository>().As<IServicesRepository>().SingleInstance();
            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            builder.Register(c => new RakPeerInterface(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5055), 10000))
                .As<INetProvider>().SingleInstance();

            builder.Register(c => new ServerNode(
                    new RakPeerInterface(externalEndpoint, 10000),
                    new RakPeerInterface(internalEndpoint, 10000),
                    c.Resolve<IMessageFactory>(), c.Resolve<IServiceProtocol>(), c.Resolve<IServicesRepository>(), c.Resolve<C2SPeer>()))
                .As<IServerNode>().SingleInstance();

            var container = builder.Build();

            var node = container.Resolve<IServerNode>();
            node.Start();

            Log.Info("Press q to quit");
            while (Console.ReadKey(true).KeyChar != 'q')
            { }

            node.Stop();
        }
    }
}
