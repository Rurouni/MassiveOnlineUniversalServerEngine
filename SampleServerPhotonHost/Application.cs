using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Autofac;
using Autofac.Integration.Mef;
using MOUSE.Core;
using PhotonAdapter;
using SampleC2SProtocol;
using SampleS2SProtocol;
using SampleServer;

namespace SampleServerPhotonHost
{
    public class ChatServer : PhotonServerHostApplication
    {
        protected override IServerNode CreateNode()
        {
            var builder = new ContainerBuilder();
            //register core messages
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(INode))));
            //register c2s contracts
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(IChatLogin))));
            //register s2s contracts
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(IChatManager))));
            //register services and generated messages
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ChatClient))));

            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
            builder.RegisterType<ServicesRepository>().As<IServicesRepository>().SingleInstance();
            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            builder.Register(c => this).As<INetProvider>();

            builder.RegisterType<ServerNode>().As<IServerNode>();

            var container = builder.Build();

            var node = container.Resolve<IServerNode>();
            return node;
        }
    }
}
