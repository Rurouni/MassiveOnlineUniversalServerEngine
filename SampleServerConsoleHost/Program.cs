using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Autofac;
using MOUSE.Core;
using RakNetWrapper;
using Autofac.Integration.Mef;
using SampleC2SProtocol;
using SampleS2SProtocol;
using SampleServer;

namespace SampleServerConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            //register core messages
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(INetPeer))));
            //register c2s messages
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(IChatLogin))));
            //register s2s messages
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(IChatManager))));
            //register services
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ChatClient))));
            
            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
            builder.RegisterType<ServicesRepository>().As<IServicesRepository>().SingleInstance();
            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            builder.Register(c=> new RakPeerInterface(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345), 10000))
                .As<INetProvider>().SingleInstance();
            
            builder.RegisterType<ServerNode>().As<ServerNode>().SingleInstance();

            var container = builder.Build();

            ServerNode node = container.Resolve<ServerNode>();

            Console.WriteLine("Press q to quit");
            while (Console.ReadKey().KeyChar !='q')
            {
            }
            
        }
    }
}
