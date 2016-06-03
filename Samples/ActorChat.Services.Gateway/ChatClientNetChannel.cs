using System;
using System.Threading.Tasks;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using MOUSE.Core;
using MOUSE.Core.Actors;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.MessageProcessing;
using IActorProxy = MOUSE.Core.Interfaces.Actors.IActorProxy;

namespace ActorChat.Services.Gateway
{
    public class ChatClientNetChannel : AsyncProcessingNetChannel
    {
        IActorProxy _room;
        string _userName;

        readonly IActorSystem<IRoomActor> _rooms;
        readonly ICallbacksGatewayNode _callbacksNode;

        public ChatClientNetChannel(IActorSystem<IRoomActor> rooms, ICallbacksGatewayNode callbacksNode, INetNode node,  
            ITransportChannel transportChannel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool) 
            : base(node, transportChannel, serializer, logger, config, bufferPool)
        {
            _rooms = rooms;
            _callbacksNode = callbacksNode;
        }

        protected override IMessageProcessingPipeBuilder Configure(IMessageProcessingPipeBuilder builder)
        {
            return builder
                //.UseIdleDisconnect(TimeSpan.FromSeconds(60*10), this)
                //.UseInvalidHandshakeDisconnect<JoinRoomS2S>(TimeSpan.FromSeconds(5))
                //.UseFiber(new ReadWriteLockingFiber())
                .UseFiber(new SimpleFiber(TaskScheduler.Default, 10)) // no more than 10 parallel request from same client
                .UseConfigurableDispatcher(config => config
                    .HandleRequestAsync<JoinRoom, JoinRoomResponse>(OnRoomJoin)
                    .HandleMessage<Say>(OnSay),

                    throwIfUnhandled: false);
        }

        async Task<JoinRoomResponse> OnRoomJoin(JoinRoom msg, IOperationContext context)
        {
            var room = await _rooms.GetActor(new ActorKey(msg.RoomName));

            var callbackRef = _callbacksNode.ExposeAsCallbackChannel(this);
            var joinRoomOnServer = new JoinRoomS2S()
            {
                CallbackChannelRef = callbackRef,
                RoomName = msg.RoomName,
                UserName = msg.UserName
            };
            var reply = await room.SendAsync<JoinRoomResponse>(joinRoomOnServer, context);
            if (reply.RetCode == JoinRoomRetCode.Ok)
            {
                _room = room;
                _userName = msg.UserName;
            }

            return reply;
        }

        void OnSay(Say msg, IOperationContext context)
        {
            if (_room != null)
            {
                _room.SendOneWay(new SayS2S(){UserName = _userName, Text = msg.Text}, context);    
            }
            else throw new ProcessingException(SayErrors.NotConnectedToRoom);
        }
    }
}