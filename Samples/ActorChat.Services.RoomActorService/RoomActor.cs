using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using MOUSE.Core;
using MOUSE.Core.Actors;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace ActorChat.Services.RoomActorService
{
    public class RoomActor : Actor, IRoomActor
    {
        readonly ICallbacksGatewayNode _node;
        readonly IReliableStateManager _stateManager;
        const int MaxRoomSize = 10000;
        readonly List<RoomMember> _roomMembers = new List<RoomMember>();
        IReliableDictionary<string, DateTime> _state;

        public RoomActor(ICallbacksGatewayNode node, IReliableStateManager stateManager)
        {
            _node = node;
            _stateManager = stateManager;
        }

        protected override IMessageProcessingLockConfigBuilder ConfigureLocks(IMessageProcessingLockConfigBuilder builder)
        {
            return base.ConfigureLocks(builder)
                .Lock<TestStateless>(LockType.Write);
        }

        protected override IMessageHandlingConfigBuilder ConfigureHandlers(IMessageHandlingConfigBuilder builder)
        {
            return builder
                .HandleRequestAsync<JoinRoomS2S, JoinRoomResponse>(Process)
                .HandleMessage<SayS2S>(Process)
                .HandleRequestAsync<TestStateless, OperationResult>(Process)
                .HandleRequestAsync<TestStateful, OperationResult>(Process);
        }

        async Task<OperationResult> Process(TestStateless msg, IOperationContext arg2)
        {
            if(msg.SleepDurationMs > 0)
                await Task.Delay(msg.SleepDurationMs);

            return OperationResult.Success;
        }

        async Task<OperationResult> Process(TestStateful msg, IOperationContext arg2)
        {
            using (var trans = _stateManager.CreateTransaction())
            {
                await _state.AddOrUpdateAsync(trans, ActorRef.Key.Id, (x) => DateTime.UtcNow, (x, _) => DateTime.UtcNow);

                await trans.CommitAsync();
            }

            return OperationResult.Success;
        }

        async public Task<JoinRoomResponse> Process(JoinRoomS2S request, IOperationContext context)
        {
            if (_roomMembers.Count >= MaxRoomSize)
                return new JoinRoomResponse { RetCode = JoinRoomRetCode.RoomIsFull };
            else
            {
                if (_roomMembers.Any(x => x.UserName == request.UserName))
                    return new JoinRoomResponse { RetCode = JoinRoomRetCode.NameIsTaken };

                _state = await _stateManager.GetOrAddAsync<IReliableDictionary<string, DateTime>>(ActorRef.Key.Id);

                try
                {
                    var callbackChannel = await _node.ConnectToCallbackChannel(request.CallbackChannelRef);

                    callbackChannel.DisconnectedEvent
                        .Subscribe(_ =>
                            Fiber.Process(() =>
                            {
                                _roomMembers.RemoveAll(x => x.UserName == request.UserName);

                                var leaveNotification = new RoomEvent { Text = "User left", UserName = request.UserName };
                                foreach (var roomMember in _roomMembers)
                                {
                                    roomMember.CallbackChannel.Send(leaveNotification);
                                }

                                if (_roomMembers.Count == 0)
                                    Dispose();

                            }));

                    var joinNotification = new RoomEvent { Text = "User joined", UserName = request.UserName };
                    foreach (var roomMember in _roomMembers)
                    {
                        roomMember.CallbackChannel.Send(joinNotification);
                    }

                    _roomMembers.Add(new RoomMember { UserName = request.UserName, CallbackChannel = callbackChannel });

                    return new JoinRoomResponse { RetCode = JoinRoomRetCode.Ok };
                }
                catch (Exception)
                {
                    return new JoinRoomResponse() { RetCode = JoinRoomRetCode.Retry };
                }
            }
        }

        public void Process(SayS2S msg, IOperationContext context)
        {
            foreach (var roomMember in _roomMembers)
            {
                roomMember.CallbackChannel.Send(new RoomEvent() { Text = msg.Text, UserName = msg.UserName });
            }
        }
    }

    public class RoomMember
    {
        public string UserName;
        public INetChannel CallbackChannel;
    }
}