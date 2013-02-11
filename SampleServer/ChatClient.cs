using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MOUSE.Core;
using MOUSE.Core.ActorCoordination;
using MOUSE.Core.Actors;
using NLog;
using Protocol.Generated;

#pragma warning disable 1998

namespace SampleServer
{
    public enum ClientState
    {
        Unregistered,
        Registered,
        InRoom
    }

    public class ChatClient : C2SPeer, IChatLogin, IChatService
    {
        private ChatUserInfo _user;

        public override void OnCreated()
        {
            Log = LogManager.GetLogger(string.Format("ChatClient<NetId:{0}>", Channel.Id));
            SetHandler<IChatLogin>(this);
            DisconnectedEvent.Subscribe(OnDisconnectAsync);
            Log.Info("connected");
        }

        public async void OnDisconnectAsync(INetPeer peer)
        {
            Log.Info("disconnected");
            if (_user != null)
            {
                ActorProxy<IUserManager> chatManager = await Node.GetActor<IUserManager>();
                chatManager.Channel.UnregisterUser(_user.Id);
            }
        }

        [NetOperationHandler]
        public async Task<LoginResult> Login(string name)
        {
            Log.Info(name + " is logging in");

            ActorProxy<IUserManager> chatManager = await Node.GetActor<IUserManager>();
            ChatUserInfo userInfo = await chatManager.Channel.TryRegisterUser(name);
            if (userInfo == null)
            {
                Log.Warn("name in use");
                return LoginResult.NameInUse;
            }

            _user = userInfo;

            SetHandler<IChatService>(this);
            RemoveHandler<IChatLogin>();

            Log = LogManager.GetLogger(string.Format("ChatClient<NetId:{0}, UserId:{1}, Name:{2}>", Channel.Id, userInfo.Id, userInfo.Name));
            Log.Info("login successfull");

            return LoginResult.Ok;
        }

        [NetOperationHandler]
        public async Task<List<string>> GetRooms()
        {
            Log.Debug("getting rooms");
            IActorCoordinator chatRoomGroupCoordinator = Node.GetCoordinator<IChatRoom>();

            IReadOnlyList<ActorRemoteInfo> actors = await chatRoomGroupCoordinator.GetActors();
            List<string> rooms = actors.Select(x => x.Name).ToList();
            return rooms;
        }

        [NetOperationHandler]
        public async Task<JoinRoomResponse> JoinOrCreateRoom(string roomName)
        {
            Log.Info("JoinOrCreateRoom -" + roomName);

            ActorProxy<IChatRoom> roomActor = await Node.GetActor<IChatRoom>(roomName);
            Log.Debug("got room service ");
            long ticket = await roomActor.Channel.AwaitUser(_user);
            Log.Info("room will await user with ticket-" + ticket);

            return new JoinRoomResponse
                {
                    RoomActorId = roomActor.Key.LocalActorId,
                    Ticket = ticket,
                    ServerEndpoint = Node.GetNode(roomActor.Key.OwnerNodeId).ExternalAddress
                };
        }

        public override string ToString()
        {
            return string.Format("ChatClient<NetId:{0}>", Channel.Id);
        }
    }
}
