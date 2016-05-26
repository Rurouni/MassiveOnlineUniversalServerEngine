using System;
using System.Threading.Tasks;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using ActorChat.Services.SFRoomActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using MOUSE.Core;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.MessageProcessing;

namespace ActorChat.Services.Gateway
{
    public class SFChatClientNetChannel : AsyncProcessingNetChannel, IRoomCallbacks
    {
        string _userName;
        private ISFRoomActor _room;


        public SFChatClientNetChannel(INetNode node, ITransportChannel transportChannel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
            : base(node, transportChannel, serializer, logger, config, bufferPool)
        {
        }

        protected override IMessageProcessingPipeBuilder Configure(IMessageProcessingPipeBuilder builder)
        {
            return builder
                //.UseIdleDisconnect(TimeSpan.FromSeconds(60 * 10), this)
                //.UseInvalidHandshakeDisconnect<JoinRoomS2S>(TimeSpan.FromSeconds(5))
                //.UseFiber(new SimpleFiber(TaskScheduler.Default, 100))
                .UseConfigurableDispatcher(config => config
                    .HandleRequestAsync<JoinRoom, JoinRoomResponse>(OnRoomJoin)
                    .HandleMessageAsync<Say>(OnSay)
                    .HandleRequestAsync<TestStateless, OperationResult>(OnTestStateless)
                    .HandleRequestAsync<TestStateful, OperationResult>(OnTestStateful),

                    throwIfUnhandled: false);
        }

        Task<OperationResult> OnTestStateless(TestStateless msg, IOperationContext context)
        {
            //return Task.FromResult(msg);
            return _room.TestStateless(msg);
        }

        Task<OperationResult> OnTestStateful(TestStateful msg, IOperationContext context)
        {
            //return Task.FromResult(msg);
            return _room.TestStateful(msg);
        }

        async Task<JoinRoomResponse> OnRoomJoin(JoinRoom msg, IOperationContext context)
        {
            var room = ActorProxy.Create<ISFRoomActor>(new ActorId(msg.RoomName));

            await room.SubscribeAsync(this);

            var joinRoomOnServer = new JoinRoomS2S()
            {
                RoomName = msg.RoomName,
                UserName = msg.UserName
            };
            var reply = await room.JoinRoom(joinRoomOnServer);
            if (reply.RetCode == JoinRoomRetCode.Ok)
            {
                _room = room;
                _userName = msg.UserName;
            }

            return reply;
        }

        async Task OnSay(Say msg, IOperationContext context)
        {
            if (_room != null)
            {
                await _room.Say(new SayS2S() { UserName = _userName, Text = msg.Text });
            }
            else throw new ProcessingException(SayErrors.NotConnectedToRoom);
        }

        public void OnRoomEvent(RoomEvent msg)
        {
            Send(msg);
        }
    }
}