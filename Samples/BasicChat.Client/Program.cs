using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using BasicChat.Protocol;
using Lidgren.Network;
using LidgrenWrap;
using MOUSE.Core;
using MOUSE.Core.Configuration;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Logging.Serilog;
using MOUSE.Core.Serialization;
using MOUSE.Core.Misc;
using Serilog;

namespace BasicChat.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //configure serialization
            var messageSerializer = new ProtobufMessageSerializer(
                new Message[] {new JoinRoom(), new Say(), new RoomEvent(), new JoinRoomResponse()},
                new MessageHeader[] {new OperationHeader()});

            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Verbose()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            var coreEventLogger = new SerilogCoreEvents(logger);

            //configure network transport provider
            var transport = new LidgrenNetProvider(
                new NetPeerConfiguration("Public") { LocalAddress = IPAddress.Loopback },
                new SerilogLidgrenEvents(logger));
            var config = new NetNodeConfig {SendTimeoutSec = 30, ConnectTimeoutSec = 60};


            var node = new NetNode<NetChannel>("client", transport, coreEventLogger, messageSerializer,config : config, 
                channelFactory: (n,transportChannel) =>
                {
                    var peer = new NetChannel(n, transportChannel, messageSerializer, coreEventLogger, config, new WcfBufferPool());
                    //low level channel handling declaration uses Rx channel.OperationReceivedEvent under the hood
                    peer.RegisterHandler<RoomEvent>(msg => Console.WriteLine(msg.UserName + "->" + msg.Text));

                    return peer;
                });

            try
            {
                Run(node).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ReadKey();
        }

        async static Task Run(INetNode node)
        {
            node.Start();
            Console.WriteLine("Input server endpoint in form of ipaddress:port");
            string[] addressAndPort = Console.ReadLine().Split(':');
            var endpoint = new IPEndPoint(IPAddress.Parse(addressAndPort[0]), int.Parse(addressAndPort[1]));

            //before doing any network comms we should connect to some server node
            using (INetChannel channel = await node.Connect(endpoint))
            {
                await Task.Yield();
                Console.WriteLine("Enter your name");
                string name = Console.ReadLine();

                Console.WriteLine("Enter chat room name");
                string roomName = Console.ReadLine();

                //using connected channel we can send in request/reply pattern
                JoinRoomResponse response = await channel.SendRequestAsync<JoinRoomResponse>(
                    new JoinRoom
                    {
                        ClientName = name,
                        RoomName = roomName
                    });

                if (response.RetCode == JoinRoomRetCode.NameIsTaken)
                {
                    Console.WriteLine("Name is taken");
                    return;
                }

                Console.WriteLine("You are connected, say something or enter exit");

                bool exit = false;
                while (!exit)
                {
                    await Task.Yield(); //client uses single threaded processing, so it's better noto block it with Console.Readline
                    string text = Console.ReadLine();
                    if (text == "exit")
                    {
                        exit = true;
                    }
                    else
                    {
                        //or we can send just one way messages
                        channel.Send(new Say {Text = text});
                    }
                }
            }
        }
    }
}
