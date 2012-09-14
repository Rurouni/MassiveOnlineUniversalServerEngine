using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Autofac;
using MOUSE.Core;
using PhotonAdapter;
using SampleServer;
using IChatLogin = Protocol.Generated.IChatLogin;

namespace SampleServerPhotonHost
{
    public class ChatServer : PhotonServerHostApplication
    {
        protected override IServerNode CreateNode()
        {
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
            builder.Register(c => this).As<INetProvider>();

            builder.RegisterType<ServerNode>().As<IServerNode>();

            var container = builder.Build();

            var node = container.Resolve<IServerNode>();
            return node;
        }
    }
}
