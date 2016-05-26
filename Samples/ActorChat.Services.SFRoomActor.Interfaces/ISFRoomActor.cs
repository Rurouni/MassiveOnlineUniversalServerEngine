using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using Microsoft.ServiceFabric.Actors;
using MOUSE.Core;

namespace ActorChat.Services.SFRoomActor.Interfaces
{
    public interface ISFRoomActor : IActor, IActorEventPublisher<IRoomCallbacks>
    {
        Task<OperationResult> TestStateless(TestStateless msg);
        Task<OperationResult> TestStateful(TestStateful msg);

        Task<JoinRoomResponse> JoinRoom(JoinRoomS2S msg);

        Task Say(SayS2S msg);

    }

    public interface IRoomCallbacks : IActorEvents
    {
        void OnRoomEvent(RoomEvent msg);
    }
}
