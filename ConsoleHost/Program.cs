using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MOUSE.Core;
using NLog;
using RakNetWrapper;
using Autofac;
using System.Net;
using Autofac.Integration.Mef;

namespace MOUSE.ConsoleHost
{
    static class Program
    {
        public static Logger Log = LogManager.GetLogger("ConsoleHost");

        static void Main(string[] args)
        {
            Log.Info("Started");
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
                var builder = new ContainerBuilder();

                builder.Register(c => new Node(NodeType.Master,
                                            c.Resolve<INetPeer>(),
                                            c.Resolve<IEntityRepository>(),
                                            c.Resolve<IEntityDomain>(),
                                            c.Resolve<IMessageFactory>(),
                                            endpoint))
                    .As<INode>().SingleInstance();

                builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>();
                builder.RegisterType<RakPeerInterface>().As<INetPeer>();
                
                //register entities, proxies and messages based on MEF export
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    builder.RegisterComposablePartCatalog(new AssemblyCatalog(assembly));
                builder.RegisterComposablePartCatalog(new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory + "\\Domain\\"));

                builder.RegisterType<EntityRepository>().As<IEntityRepository>().SingleInstance();
                builder.RegisterType<EntityDomain>().As<IEntityDomain>().SingleInstance();
                builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();

                IContainer container = builder.Build();
                
                var node = container.Resolve<INode>();
                node.Start(manualUpdate:false);

                while (true)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Log.FatalException("Critical Error, Host will be closed", ex);
            }
            Log.Info("Exited");
        }
    }
}
