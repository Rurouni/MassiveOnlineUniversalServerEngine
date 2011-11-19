//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.ComponentModel.Composition.Hosting;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading;
//using MOUSE.Core;
//using NLog;
//using RakNetWrapper;
//using System.Diagnostics;
//using System.Net;
//using SampleProtocol;
//using Autofac;
//using Autofac.Integration.Mef;


//namespace SimpleClient
//{
//    class Program
//    {
//        private static int RequestId = 0;
//        public static INode node;
//        static void OnException(Exception ex)
//        {
//            Console.WriteLine(ex.ToString());                
//        }

//        static void Main(string[] args)
//        {
//            var masterEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
//            var builder = new ContainerBuilder();

//            builder.Register(c => new Node(NodeType.Client,
//                                        c.Resolve<INetPeer>(),
//                                        c.Resolve<IEntityRepository>(),
//                                        c.Resolve<IEntityDomain>(),
//                                        c.Resolve<IMessageFactory>(),
//                                        null, masterEndpoint))
//                .As<INode>().SingleInstance();

//            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>();
//            builder.RegisterType<RakPeerInterface>().As<INetPeer>();

//            //register entities, proxies and messages based on MEF export
//            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
//                builder.RegisterComposablePartCatalog(new AssemblyCatalog(assembly));

//            builder.RegisterComposablePartCatalog(new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory + "\\Domain\\"));

//            builder.RegisterType<EntityRepository>().As<IEntityRepository>().SingleInstance();
//            builder.RegisterType<EntityDomain>().As<IEntityDomain>().SingleInstance();
//            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();

//            IContainer container = builder.Build();

//            node = container.Resolve<INode>();
//            node.Start(manualUpdate:true);

//            Console.WriteLine("enter help");
//            var sb = new StringBuilder();
            
//            //imagine this is game loop
//            while (true)
//            {
//                node.Update();
//                if(Console.KeyAvailable)
//                {
//                    var key = Console.ReadKey();
//                    if (key.Key == ConsoleKey.Enter)
//                    {
//                        OnCommand(sb.ToString());
//                        sb.Clear();
//                    }
//                    else
//                    {
//                        sb.Append(key.KeyChar);
//                    }
//                }
//                Thread.Sleep(1);
//            }
            
//        }

//        private static void OnCommand(string input)
//        {
//            string[] words = input.Split(' ');
//            if (words.Length > 0)
//            {
//                string command = words[0];
//                switch (command)
//                {
//                    case "help": Console.WriteLine("connect <ip> <port>\nshow"); break;
//                    case "ping": Ping(); break;
                        
//                    default: Console.WriteLine("unknown command, type help for list of commands"); break;
//                }
//            }
//        }

//        private static async void Ping()
//        {
//            var timer = new Stopwatch();
//            timer.Start();
//            var entity = node.GetProxy<ISampleEntity>();
//            int replyId = await entity.Ping(RequestId++);
//            Console.WriteLine("succesfully pinged responseId:{0}, timeMs:{1}", replyId, timer.ElapsedTicks / Stopwatch.Frequency * 1000);
//        }
//    }
//}
