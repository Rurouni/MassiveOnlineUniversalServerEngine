using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
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

        public override void Init(INetChannel channel, INetNode<INetPeer> owner)
        {
            base.Init(channel, owner);

            SetHandler<IChatLogin>(this);
            DisconnectedEvent.Subscribe(OnDisconnectAsync);
        }

        public async void OnDisconnectAsync(INetPeer peer)
        {
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
            if (_state != ClientState.Unregistered)
                return LoginResult.AlreadyRegistered;

            IChatManager chatManager = await Node.GetService<IChatManager>();
            ChatUserInfo userInfo = await chatManager.TryRegisterUser(name);
            if (userInfo == null)
                return LoginResult.NameInUse;

            _user = userInfo;
            _state = ClientState.Registered;
            SetHandler<IChatService>(this);
            return LoginResult.Ok;

        }

        public async Task<List<ChatRoomInfo>> GetRooms()
        {
            IChatManager chatManager = await Node.GetService<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            return rooms;
        }

        public async Task<CreateRoomResponse> JoinOrCreateRoom(string roomName)
        {
            IChatManager chatManager = await Node.GetService<IChatManager>();
            _roomId = await chatManager.GetOrCreateRoom(roomName);

            IChatRoom room = await Node.GetService<IChatRoom>(_roomId);
            long ticket = await room.AwaitUser(_user);

            return new CreateRoomResponse
                {
                    RoomId = _roomId,
                    Ticket = ticket
                };
        }

        public async Task<long> JoinRoom(uint roomId)
        {
            IChatManager chatManager = await Node.GetService<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            if(rooms.Any(x=>x.Id == roomId))
            {
                IChatRoom room = await Node.GetService<IChatRoom>(roomId);
                _roomId = roomId;
                long ticket = await room.AwaitUser(_user);
                return ticket;
            }
            throw new InvalidInput(JoinRoomInvalidRetCode.RoomNotFound);
        }
    }
}
