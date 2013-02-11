using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Autofac;
using MOUSE.Core;
using MOUSE.Core.Actors;
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
                .Where(x => x.IsAssignableTo<NetProxy>() && x != typeof(NetProxy))
                .As<NetProxy>();

            //register domain service implementations
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(ChatManager)))
                .Where(x => x.IsAssignableTo<Actor>() && x != typeof(Actor))
                .As<Actor>();

            builder.RegisterType<ChatClient>().As<C2SPeer>();

            builder.RegisterType<OperationDispatcher>().As<IOperationDispatcher>().SingleInstance();
            builder.RegisterType<ActorRepository>().As<IActorRepository>().SingleInstance();
            builder.RegisterType<NullKeyValueStorage>().As<IKeyValueStorage>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            builder.Register(c => this).As<INetProvider>();

            builder.RegisterType<ServerNode>().As<IServerNode>();

            var container = builder.Build();

            var node = container.Resolve<IServerNode>();
            return node;
        }
    }
}
