using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BasicChat.Protocol;
using Lidgren.Network;
using LidgrenWrap;
using Microsoft.ServiceFabric.Data;
using MOUSE.Core;
using MOUSE.Core.Azure.ServiceFabric;
using MOUSE.Core.Configuration;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Logging;
using MOUSE.Core.Logging.Serilog;
using MOUSE.Core.MessageProcessing;
using MOUSE.Core.Serialization;
using Serilog;

namespace ServiceFabric.BasicChatServer
{
    internal sealed class BasicChatServer : StatefulService
    {
        readonly Dictionary<INetChannel, RoomMember> RoomMembers = new Dictionary<INetChannel, RoomMember>();
        readonly Dictionary<string, RoomData> Rooms = new Dictionary<string, RoomData>();

        //we are using this fiber as global synchronization point
        static readonly ReadWriteLockingFiber Fiber = new ReadWriteLockingFiber();
        IReliableDictionary<string, RoomData> _roomsState;

        public BasicChatServer(StatefulServiceContext serviceContext) : base(serviceContext){}

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new [] { new ServiceReplicaListener(initParams => new ServiceFabricCommunicationListenerAdapter(CreateNetNode(initParams))) };
        }

        INetNode CreateNetNode(StatefulServiceContext initParams)
        {
            var logger = new LoggerConfiguration()
                .ConfigureMOUSETypesDestructure()
                .MinimumLevel.Verbose()
                .WriteTo.Seq("http://localhost:5341/")
                .CreateLogger();

            var coreEventsLogger = new SerilogCoreEvents(logger);
            var lidgrenEventsLogger = new SerilogLidgrenEvents(logger);


            var messageSerializer = new ProtobufMessageSerializer(typeof(Message).Assembly, typeof(Say).Assembly);

            var endPoint = initParams
                .CodePackageActivationContext
                .GetEndpoint("ServiceEndPoint");

            var nodeContext = FabricRuntime.GetNodeContext();

            string nodeName = $"{nodeContext.NodeName}:{nodeContext.NodeId}";

            var transport = new LidgrenNetProvider(
                new NetPeerConfiguration("Public")
                {
                    MaximumConnections = 10,
                    AcceptIncomingConnections = true,
                    Port = endPoint.Port,
                    LocalAddress = Dns.GetHostAddresses(nodeContext.IPAddressOrFQDN).First(x => x.AddressFamily == AddressFamily.InterNetwork)
                },
                lidgrenEventsLogger);

            var netConfig = new NetNodeConfig()
            {
                ConnectTimeoutSec = 30,
                SendTimeoutSec = 30
            };

            var bufferPool = new WcfBufferPool();
            
            var node = new NetNode<NetChannel>(nodeName, transport, coreEventsLogger, messageSerializer,
                    config: netConfig,
                    channelFactory: (owner, transportChannel) =>
                        new AsyncProcessingNetChannel(builder => builder
                            .UseFiber(Fiber, config => config
                                .Lock<Say>(LockType.Read)
                                .Lock<JoinRoom>(LockType.Write)
                                .Lock<PeerDisconnected>(LockType.Write))
                            .UseConfigurableDispatcher(config => config
                                .HandleMessageAsync<Say>(OnSay)
                                .HandleMessageAsync<JoinRoom>(OnRoomJoin)
                                .HandleMessageAsync<PeerDisconnected>((msg, o) => OnRoomLeave(o.ReplyChannel))),
                            owner, transportChannel, messageSerializer, coreEventsLogger, netConfig, bufferPool));

            return node;
        }

        async protected override Task RunAsync(CancellationToken cancelServicePartitionReplica)
        {
            _roomsState = await StateManager.GetOrAddAsync<IReliableDictionary<string, RoomData>>("Rooms");
            using (var tx = StateManager.CreateTransaction())
            {
                var roomEnumerable = await _roomsState.CreateEnumerableAsync(tx, EnumerationMode.Unordered);
                using (var roomEnumerator = roomEnumerable.GetAsyncEnumerator())
                {
                    while (await roomEnumerator.MoveNextAsync(cancelServicePartitionReplica).ConfigureAwait(false))
                    {
                        roomEnumerator.Current.Value.Members = new List<RoomMember>();
                        Rooms.Add(roomEnumerator.Current.Key, roomEnumerator.Current.Value);
                    }
                }
            }
            
            ServiceEventSource.Current.ServiceMessage(this, "RunAsync called");
            while (!cancelServicePartitionReplica.IsCancellationRequested)
            {
                await Task.Delay(10000, cancelServicePartitionReplica);
            }
        }


        async Task OnSay(Say msg, IOperationContext context)
        {
            RoomMember member;
            if (RoomMembers.TryGetValue(context.ReplyChannel, out member))
            {
                RoomData room;
                if (Rooms.TryGetValue(member.RoomName, out room))
                {
                    var newEvent = new RoomEvent() {UserName = member.UserName, Text = msg.Text};
                    room.History.Add(newEvent);

                    using (var tx = StateManager.CreateTransaction())
                    {
                        await _roomsState.SetAsync(tx, room.Name, room);
                        await tx.CommitAsync();
                    }

                    room.Members.ForEach(m => m.UserChannel.Send(newEvent));
                }
            }
        }

        async Task OnRoomJoin(JoinRoom msg, IOperationContext context)
        {
            var newMember = new RoomMember
            {
                RoomName = msg.RoomName,
                UserName = msg.ClientName,
                UserChannel = context.ReplyChannel
            };

            var newEvent = new RoomEvent() { UserName = msg.ClientName, Text = "Joined" };

            RoomData room;
            if (!Rooms.TryGetValue(msg.RoomName, out room))
            {
                room = new RoomData(msg.RoomName);
                room.Members = new List<RoomMember> { newMember };
                
                Rooms.Add(msg.RoomName, room);
            }
            else
            {
                if (room.Members.Any(x => x.UserName == msg.ClientName))
                {
                    context.ReplyChannel.Reply(context, new JoinRoomResponse() { RetCode = JoinRoomRetCode.NameIsTaken });
                    return;
                }
                else
                {
                    room.Members.ForEach(m => m.UserChannel.Send(newEvent));
                    room.Members.Add(newMember);
                }
            }

            RoomMembers.Add(context.ReplyChannel, newMember);
            room.History.Add(newEvent);

            using (var tx = StateManager.CreateTransaction())
            {
                await _roomsState.SetAsync(tx, room.Name, room);
                await tx.CommitAsync();
            }

            context.ReplyChannel.Reply(context, new JoinRoomResponse() { RetCode = JoinRoomRetCode.Ok });

            foreach (var roomEvent in room.History)
            {
                context.ReplyChannel.Send(roomEvent);
            }
        }

        async Task OnRoomLeave(INetChannel channel)
        {
            RoomMember member;
            if (RoomMembers.TryGetValue(channel, out member))
            {
                var newEvent = new RoomEvent() { UserName = member.UserName, Text = "Left" };
                RoomData room;
                if (Rooms.TryGetValue(member.RoomName, out room))
                {
                    room.Members.Remove(member);
                    room.Members.ForEach(m => m.UserChannel.Send(newEvent));

                    room.History.Add(newEvent);

                    using (var tx = StateManager.CreateTransaction())
                    {
                        await _roomsState.SetAsync(tx, room.Name, room);
                        await tx.CommitAsync();
                    }
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

    [DataContract]
    public class RoomData
    {
        [DataMember(Order = 1)]
        public string Name;

        [DataMember(Order = 2)]
        public List<RoomEvent> History = new List<RoomEvent>();

        public List<RoomMember> Members = new List<RoomMember>();

        public RoomData(string name)
        {
            Name = name;
        }
    }

}
