using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ActorChat.Gateway.Protocol;
using Lidgren.Network;
using LidgrenWrap;
using MOUSE.Core;
using MOUSE.Core.Actors;
using MOUSE.Core.Configuration;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.Logging.Serilog;
using MOUSE.Core.Misc;
using MOUSE.Core.Serialization;
using Serilog;

namespace ActorChat.Client
{
    static class Program
    {
        static void Main(string[] args)
        {
            //configure serialization
            var messageSerializer = new ProtobufMessageSerializer(typeof(Message).Assembly, typeof(JoinRoom).Assembly);
            
            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Error()
                .WriteTo.ColoredConsole()
                .CreateLogger();
            
            var coreEventLogger = new SerilogCoreEvents(logger);

            //configure network transport provider
            

            Action<ISimpleMessageHandlingConfigBuilder> messageHandlingConfigurator =
                c => c.HandleMessage<RoomEvent>(OnRoomEvent);

            //Console.WriteLine("Connect to");

            Console.WriteLine("Enter how many clients to run simultaneously");
            var clientCount = int.Parse(Console.ReadLine());

            string roomName = Guid.NewGuid().ToString();


            for (int i = 0; i < clientCount; i++)
            {
                var transport = new LidgrenNetProvider(
                    new NetPeerConfiguration("Public")
                    {
                        ConnectionTimeout = 30
                    },
                    new SerilogLidgrenEvents(logger));
                var config = new NetNodeConfig { SendTimeoutSec = 30, ConnectTimeoutSec = 30 };
                var node = new ClientNode(
                //new[] { "127.0.0.1:20001" },

                new[] { "rurounisfcluster.westeurope.cloudapp.azure.com:20001" },
                transport, coreEventLogger, messageSerializer, messageHandlingConfigurator, config: config);

                try
                {
                    node.Start();
                    node.ExecuteChatUserFlow(roomName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            Thread.Sleep(-1);
        }

        static void OnRoomEvent(RoomEvent msg)
        {
            Console.WriteLine(msg.UserName + "->" + msg.Text);
        }

        async static Task ExecuteChatUserFlow(this ClientNode node, string roomName)
        {
            string name = Guid.NewGuid().ToString();


            JoinRoomResponse response = await node.SendRequestAsync<JoinRoomResponse>(
                new JoinRoom
                {
                    UserName = name,
                    RoomName = roomName
                });

            if (response.RetCode == JoinRoomRetCode.NameIsTaken)
            {
                Console.WriteLine("Name is taken");
                return;
            }

            Console.WriteLine("You are connected, say something or write 'exit' to quit");
            bool exit = false;

            const int MaxMessages = 10;
            while (!exit)
            {
                //await Task.Yield(); //client uses single threaded processing, so it's better noto block it with Console.Readline
                //string text = Console.ReadLine();
                //if (text == "exit")
                //{
                //    exit = true;
                //}
                //else
                //{
                //    node.SendOneWay(new Say { Text = text });
                //}
                var timer = Stopwatch.StartNew();
                List<Task<OperationResult>> tasks = new List<Task<OperationResult>>(MaxMessages);
                for (int i = 0; i < MaxMessages; i++)
                {
                    tasks.Add(node.SendRequestAsync<OperationResult>(new TestStateless()));
                }

                await Task.WhenAll(tasks);
                Console.WriteLine(timer.ElapsedMilliseconds);

                //Thread.Sleep(1000);
            }
        }
    }
}
