using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Lidgren.Network;
using LidgrenWrap;
using MOUSE.Core;
using MOUSE.Core.Configuration;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.MessageProcessing;
using MOUSE.Core.Serialization;
using System.Linq;
using BasicChat.Protocol;
using MOUSE.Core.Logging.Serilog;
using Serilog;

namespace BasicChat.Server
{
    class Program
    {
        //in memory state for Rooms and members
        static readonly Dictionary<INetChannel, RoomMember> RoomMembers = new Dictionary<INetChannel, RoomMember>();
        static readonly Dictionary<string, List<RoomMember>> Rooms = new Dictionary<string, List<RoomMember>>();

        //we are using this fiber as global synchronization point
        static readonly ReadWriteLockingFiber Fiber = new ReadWriteLockingFiber();
        
        static void Main(string[] args)
        {
            int port = 12345;

            var messageSerializer = new ProtobufMessageSerializer(
                new Message[] { new JoinRoom(), new Say(), new RoomEvent(), new JoinRoomResponse() },
                new MessageHeader[] { new OperationHeader() });

            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Verbose()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            var coreEventLogger = new SerilogCoreEvents(logger);

            var transport = new LidgrenNetProvider(
                new NetPeerConfiguration("Public")
                {
                    MaximumConnections = 10,
                    AcceptIncomingConnections = true,
                    Port = port,
                    LocalAddress = IPAddress.Loopback
                },
                new SerilogLidgrenEvents(logger));

            var netConfig = new NetNodeConfig()
            {
                ConnectTimeoutSec = 30,
                SendTimeoutSec = 30
            };

            var bufferPool = new WcfBufferPool();

            var node = new NetNode<NetChannel>("server", transport, coreEventLogger, messageSerializer, config: netConfig,
                channelFactory: (n, transportChannel) =>
                    new AsyncProcessingNetChannel(builder => builder
                        .UseFiber(Fiber, config => config
                            .Lock<Say>(LockType.Read)
                            .Lock<JoinRoom>(LockType.Write)
                            .Lock<PeerDisconnected>(LockType.Write))
                        .UseConfigurableDispatcher(config => config
                            .HandleMessage<Say>(OnSay)
                            .HandleMessage<JoinRoom>(OnRoomJoin)
                            .HandleMessage<PeerDisconnected>((msg, o) => OnRoomLeave(o.ReplyChannel))),
                        n, transportChannel, messageSerializer, coreEventLogger, netConfig, bufferPool));
            
            node.Start();
            Console.ReadKey();
        }

        static void OnSay(Say msg, IOperationContext context)
        {
            RoomMember member;
            if (RoomMembers.TryGetValue(context.ReplyChannel, out member))
            {
                List<RoomMember> members;
                if (Rooms.TryGetValue(member.RoomName, out members))
                {
                    members.ForEach((m) => m.UserChannel.Send(new RoomEvent() { UserName = member.UserName, Text = msg.Text }));
                }    
            }
        }

        static void OnRoomJoin(JoinRoom msg, IOperationContext context)
        {
            var newMember = new RoomMember
            {
                RoomName = msg.RoomName,
                UserName = msg.ClientName,
                UserChannel = context.ReplyChannel
            };
            List<RoomMember> members;
            if (!Rooms.TryGetValue(msg.RoomName, out members))
            {
                
                members = new List<RoomMember> { newMember };
                Rooms.Add(msg.RoomName, members);
            }
            else
            {
                if (members.Any(x => x.UserName == msg.ClientName))
                {
                    context.ReplyChannel.Reply(context, new JoinRoomResponse(){RetCode = JoinRoomRetCode.NameIsTaken});
                    return;
                }
                else
                {
                    members.ForEach((m) => m.UserChannel.Send(new RoomEvent(){UserName = msg.ClientName, Text = "Joined"}));
                    members.Add(newMember);
                }
            }
            RoomMembers.Add(context.ReplyChannel, newMember);

            context.ReplyChannel.Reply(context, new JoinRoomResponse() { RetCode = JoinRoomRetCode.Ok });
        }


        static void OnRoomLeave(INetChannel channel)
        {
            RoomMember member;
            if (RoomMembers.TryGetValue(channel, out member))
            {
                List<RoomMember> members;
                if (Rooms.TryGetValue(member.RoomName, out members))
                {
                    members.Remove(member);
                    members.ForEach((m) => m.UserChannel.Send(new RoomEvent() { UserName = member.UserName, Text = "Left" }));
                }
            }
        }
    }

    public class RoomMember
    {
        public string RoomName;
        public string UserName;
        public INetChannel UserChannel;
    }
}
