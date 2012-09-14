using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using NLog;
using Protocol.Generated;

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
                IChatManager chatManager = await Node.GetService<IChatManager>();
                chatManager.UnregisterUser(_user.Id);
            }
        }

        [NetOperationHandler]
        public async Task<LoginResult> Login(string name)
        {
            Log.Info(name + " is logging in");

            IChatManager chatManager = await Node.GetService<IChatManager>();
            ChatUserInfo userInfo = await chatManager.TryRegisterUser(name);
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
        public async Task<List<ChatRoomInfo>> GetRooms()
        {
            Log.Debug("getting rooms");
            IChatManager chatManager = await Node.GetService<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            return rooms;
        }

        [NetOperationHandler]
        public async Task<JoinRoomResponse> JoinOrCreateRoom(string roomName)
        {
            Log.Info("JoinOrCreateRoom -" + roomName);

            IChatManager chatManager = await Node.GetService<IChatManager>();
            uint roomId = await chatManager.GetOrCreateRoom(roomName);
            Log.Info("joining roomId:" + roomId);

            IChatRoom room = await Node.GetService<IChatRoom>(roomId);
            Log.Debug("got room service ");
            long ticket = await room.AwaitUser(_user);
            Log.Info("room will await user with ticket-" + ticket);

            return new JoinRoomResponse
                {
                    RoomId = roomId,
                    Ticket = ticket,
                    ServerEndpoint = Node.ExternalNet.Endpoint.ToString()
                };
        }

        [NetOperationHandler]
        public async Task<JoinRoomResponse> JoinRoom(uint roomId)
        {
            Log.Info("JoinRoom -" + roomId);

            IChatManager chatManager = await Node.GetService<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            
            if (!rooms.Any(x => x.Id == roomId))
                throw new InvalidInput(JoinRoomInvalidRetCode.RoomNotFound);
         
            IChatRoom room = await Node.GetService<IChatRoom>(roomId);
            long ticket = await room.AwaitUser(_user);
            Log.Info("room will await user with ticket-" + ticket);
            return new JoinRoomResponse
            {
                RoomId = roomId,
                Ticket = ticket,
                ServerEndpoint = Node.ExternalNet.Endpoint.ToString()
            };
        }

        public override string ToString()
        {
            return string.Format("ChatClient<NetId:{0}>", Channel.Id);
        }
    }
}
