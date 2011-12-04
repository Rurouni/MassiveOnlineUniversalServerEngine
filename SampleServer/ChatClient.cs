using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using NLog;
using SampleC2SProtocol;
using SampleS2SProtocol;

namespace SampleServer
{
    public enum ClientState
    {
        Unregistered,
        Registered,
        InRoom
    }

    [Export(typeof(ClientPeer))]
    public class ChatClient : ClientPeer, IChatLogin, IChatService
    {
        ClientState _state;
        private ChatUserInfo _user;
        uint _roomId;

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
            if (_state != ClientState.Unregistered)
            {
                IChatManager chatManager = await Node.GetService<IChatManager>();
                chatManager.UnregisterUser(_user.Id);
            }
            if(_state == ClientState.InRoom)
            {
                IChatRoom room = await Node.GetService<IChatRoom>(_roomId);
                room.RemoveUser(_user.Id);
            }
        }

        public async Task<LoginResult> Login(string name)
        {
            Log.Info(name + " is logging in");
            if (_state != ClientState.Unregistered)
                return LoginResult.AlreadyRegistered;

            IChatManager chatManager = await Node.GetService<IChatManager>();
            ChatUserInfo userInfo = await chatManager.TryRegisterUser(name);
            if (userInfo == null)
            {
                Log.Warn("name in use");
                return LoginResult.NameInUse;
            }

            _user = userInfo;
            _state = ClientState.Registered;
            SetHandler<IChatService>(this);
            Log = LogManager.GetLogger(string.Format("ChatClient<NetId:{0}, UserId:{1}, Name:{2}>", Channel.Id, userInfo.Id, userInfo.Name));
            Log.Info("login successfull");
            return LoginResult.Ok;

        }

        public async Task<List<ChatRoomInfo>> GetRooms()
        {
            Log.Debug("getting rooms");
            IChatManager chatManager = await Node.GetService<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            return rooms;
        }

        public async Task<CreateRoomResponse> JoinOrCreateRoom(string roomName)
        {
            Log.Info("JoinOrCreateRoom -" + roomName);
            if(_roomId != 0)
            {
                Log.Info("already in room -" + _roomId);
                IChatRoom existingRoom = await Node.GetService<IChatRoom>(_roomId);
                await existingRoom.RemoveUser(_user.Id);
            }
            IChatManager chatManager = await Node.GetService<IChatManager>();
            _roomId = await chatManager.GetOrCreateRoom(roomName);
            Log.Info("joining roomId:{0} -" + _roomId);

            IChatRoom room = await Node.GetService<IChatRoom>(_roomId);
            long ticket = await room.AwaitUser(_user);
            Log.Info("room will await user with ticket-" + ticket);

            return new CreateRoomResponse
                {
                    RoomId = _roomId,
                    Ticket = ticket
                };
        }

        public async Task<long> JoinRoom(uint roomId)
        {
            Log.Info("JoinRoom -" + roomId);
            if (_roomId != 0)
            {
                Log.Info("already in room -" + _roomId);
                IChatRoom existingRoom = await Node.GetService<IChatRoom>(_roomId);
                await existingRoom.RemoveUser(_user.Id);
            }
            IChatManager chatManager = await Node.GetService<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            if(rooms.Any(x=>x.Id == roomId))
            {
                IChatRoom room = await Node.GetService<IChatRoom>(roomId);
                _roomId = roomId;
                long ticket = await room.AwaitUser(_user);
                Log.Info("room will await user with ticket-" + ticket);
                return ticket;
            }
            throw new InvalidInput(JoinRoomInvalidRetCode.RoomNotFound);
        }

        public override string ToString()
        {
            return string.Format("ChatClient<NetId:{0}>", Channel.Id);
        }
    }
}
