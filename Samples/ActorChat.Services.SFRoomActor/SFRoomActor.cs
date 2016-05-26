using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using ActorChat.Backend.Protocol;
using ActorChat.Gateway.Protocol;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using ActorChat.Services.SFRoomActor.Interfaces;
using MOUSE.Core;
using MOUSE.Core.Interfaces;

namespace ActorChat.Services.SFRoomActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Volatile)]
    internal class SFRoomActor : Actor, ISFRoomActor
    {
        const int MaxRoomSize = 100000;
        readonly List<RoomMember> _roomMembers = new List<RoomMember>();
        private IRoomCallbacks _callbackChannel;

        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
            _callbackChannel = GetEvent<IRoomCallbacks>();

            return Task.FromResult(0);
        }

        async public Task<OperationResult> TestStateless(TestStateless msg)
        {
            if (msg.SleepDurationMs > 0)
                await Task.Delay(msg.SleepDurationMs);

            return OperationResult.Success;
        }

        async public Task<OperationResult> TestStateful(TestStateful msg)
        {
            await StateManager.SetStateAsync<DateTime>("Dict", DateTime.UtcNow);
            return OperationResult.Success;
        }

        async public Task<JoinRoomResponse> JoinRoom(JoinRoomS2S request)
        {
            if (_roomMembers.Count >= MaxRoomSize)
                return new JoinRoomResponse { RetCode = JoinRoomRetCode.RoomIsFull };
            else
            {
                if (_roomMembers.Any(x => x.UserName == request.UserName))
                    return new JoinRoomResponse { RetCode = JoinRoomRetCode.NameIsTaken };

                try
                {

                    var joinNotification = new RoomEvent { Text = "User joined", UserName = request.UserName };
                    _callbackChannel.OnRoomEvent(joinNotification);

                    _roomMembers.Add(new RoomMember { UserName = request.UserName });

                    return new JoinRoomResponse { RetCode = JoinRoomRetCode.Ok };
                }
                catch (Exception)
                {
                    return new JoinRoomResponse() { RetCode = JoinRoomRetCode.Retry };
                }
            }
        }

        public Task Say(SayS2S msg)
        {
            _callbackChannel.OnRoomEvent(new RoomEvent() { Text = msg.Text, UserName = msg.UserName });
            return Task.FromResult(0);
        }
    }

    public class RoomMember
    {
        public string UserName;
    }
}
