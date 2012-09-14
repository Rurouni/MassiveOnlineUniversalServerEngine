using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Autofac;
using ExitGames.Client.Photon;
using MOUSE.Core;
using Protocol.Generated;
using UnityClient;

namespace UnityClientTester
{
    class Tester : IChatRoomServiceCallback
    {
        static void Main(string[] args)
        {
            new Tester().Run();
        }

        public void Run()
        {
            var builder = new ContainerBuilder();

            //register core messages
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof (EmptyMessage)))
                .Where(x => x.IsAssignableTo<Message>() && x != typeof (Message))
                .As<Message>();

            //register domain messages
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof (IChatLogin)))
                .Where(x => x.IsAssignableTo<Message>() && x != typeof (Message))
                .As<Message>();

            //register domain service definitions and proxies
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof (IChatLogin)))
                .Where(x => x.IsAssignableTo<NodeServiceProxy>() && x != typeof (NodeServiceProxy))
                .As<NodeServiceProxy>();

            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            builder.Register(
                c =>
                new PhotonClient(c.Resolve<IMessageFactory>(), c.Resolve<IServiceProtocol>(), ConnectionProtocol.Udp,
                                 "MouseChat"))
                .As<INetClient>();
            var container = builder.Build();

            var client = container.Resolve<INetClient>();
            //set callback handlers
            client.SetHandler<IChatRoomServiceCallback>(this);
            client.OnDisconnected.Subscribe((_) => Console.WriteLine("OnDisconnected"));
            client.OnConnected.Subscribe((_) => Console.WriteLine("OnConnected"));
            client.OnMessage.Subscribe((msg) => Console.WriteLine("OnReceived " + msg));

            foreach (var o in client.Connect("127.0.0.1:5055"))
            {
                client.Update();
                Thread.Sleep(10);
            }

            var loginService = client.GetService<IChatLogin>();
            var loginReply = new OperationReply<LoginResult>();

            foreach (var o in loginService.Login("UnityTester", loginReply))
            {
                client.Update();
                Thread.Sleep(10);
            }

            Console.WriteLine("Login Reply:" + loginReply.Reply);

            var chatServiceProxy = client.GetService<IChatService>();
            var joinReply = new OperationReply<JoinRoomResponse>();

            foreach (var o in chatServiceProxy.JoinOrCreateRoom("TestRoom", joinReply))
            {
                client.Update();
                Thread.Sleep(10);
            }

            Console.WriteLine("CreateRoom RoomId:" + joinReply.Reply.RoomId);

            var chatRoomServiceProxy = client.GetService<IChatRoomService>(joinReply.Reply.RoomId);
            var roomContent = new OperationReply<List<string>>();

            foreach (var o in chatRoomServiceProxy.Join(joinReply.Reply.Ticket, roomContent))
            {
                client.Update();
                Thread.Sleep(10);
            }
            


            foreach (var msg in roomContent.Reply)
                Console.WriteLine(msg);
            while (true)
            {
                client.Update();
                Thread.Sleep(10);
            }
            
        }

        public void OnRoomMessage(uint roomId, string message)
        {
            Console.WriteLine("OnRoomMessage RoomId:" + roomId + " Message:" + message);
        }
    }
}
