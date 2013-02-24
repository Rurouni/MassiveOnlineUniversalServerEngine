using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Lidgren.Network;
using LidgrenWrap;
using MOUSE.Unity;
using Protocol.Generated;
using NetPeer = MOUSE.Unity.NetPeer;

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


            var netConfiguration = new NetPeerConfiguration("ChatApp")
            {
                ConnectionTimeout = 10000,
            };
            var messageFactory = new MessageFactory(ProtocolDescription.GetAllMessages());
            var dispatcher = new OperationDispatcher(messageFactory, ProtocolDescription.GetAllProxies());
            var client = new NetNode<NetPeer>(new LidgrenNetProvider(netConfiguration), messageFactory, dispatcher);
            client.Start();

            client.PeerDisconnectedEvent.Subscribe((_) => Console.WriteLine("OnDisconnected"));
            client.PeerConnectedEvent.Subscribe((_) => Console.WriteLine("OnConnected"));
            
            var connFuture = client.ConnectToServer("127.0.0.1:5055");

            while (connFuture.State == FutureState.InProgress)
            {
                client.Update();
                Thread.Sleep(10);
            }
            NetPeer peer = connFuture.Result;
            peer.MessageEvent.Subscribe((msg) => Console.WriteLine("OnReceived " + msg));
            var loginService = peer.GetProxy<IChatLogin>();
            
            var loginFuture = loginService.Login("UnityTester");
            while (loginFuture.State == FutureState.InProgress)
            {
                client.Update();
                Thread.Sleep(10);
            }

            Console.WriteLine("Login Reply:" + loginFuture.Result);

            var chatServiceProxy = peer.GetProxy<IChatService>();
            var joinRoomFuture = chatServiceProxy.JoinOrCreateRoom("TestRoom");
            while (joinRoomFuture.State == FutureState.InProgress)
            {
                client.Update();
                Thread.Sleep(10);
            }

            Console.WriteLine("CreateRoom RoomId:" + joinRoomFuture.Result.RoomActorId);
            connFuture = client.ConnectToServer(joinRoomFuture.Result.ServerEndpoint);
            while (connFuture.State == FutureState.InProgress)
            {
                client.Update();
                Thread.Sleep(10);
            }
            
            var roomPeer = connFuture.Result;
            roomPeer.SetHandler<IChatRoomServiceCallback>(this);
            var chatRoomServiceProxy = roomPeer.GetProxy<IChatRoomService>(joinRoomFuture.Result.RoomActorId);

            var connectRoomFuture = chatRoomServiceProxy.Join(joinRoomFuture.Result.Ticket);

            while (connectRoomFuture.State == FutureState.InProgress)
            {
                client.Update();
                Thread.Sleep(10);
            }

            foreach (var msg in connectRoomFuture.Result)
                Console.WriteLine(msg);

            
            while (true)
            {
                client.Update();
                Thread.Sleep(10);
            }
            
        }

        public void OnRoomMessage(string roomName, string message)
        {
            Console.WriteLine("OnRoomMessage Room:" + roomName + " Message:" + message);
        }
    }
}
