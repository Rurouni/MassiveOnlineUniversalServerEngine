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

    [Export(typeof(ClientNodePeer))]
    public class ChatClient : ClientNodePeer, IChatLogin, IChatService
    {
        string _name;
        ClientState _state;
        uint _userId;
        uint _roomId;

        public ChatClient(INetChannel channel, ServerNode node) : base(channel, node)
        {
            SetHandler<IChatLogin>(this);
            DisconnectedEvent.Subscribe(OnDisconnect);
        }

        public async void OnDisconnect(INetPeer peer)
        {
            if (_state != ClientState.Unregistered)
            {
                IChatManager chatManager = await Node.GetService<IChatManager>();
                chatManager.UnregisterUser(_userId);
            }
            if(_state == ClientState.InRoom)
            {
                IChatRoom room = await Node.GetService<IChatRoom>();
                room.RemoveUser(_userId);
            }
        }

        public async Task<LoginResult> Login(string name)
        {
            if (_state != ClientState.Unregistered)
                return LoginResult.InvalidRequest;

            _name = name;
            IChatManager chatManager = await Node.GetService<IChatManager>();
            ChatUserInfo userInfo = await chatManager.TryRegisterUser(name);
            if (userInfo == null)
                return LoginResult.NameInUse;

            _userId = userInfo.Id;
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

        public async Task<CreateRoomResponse> CreateRoom(string roomName)
        {
            IChatManager chatManager = await Node.GetService<IChatManager>();
            uint roomId = await chatManager.CreateRoom(roomName);

            IChatRoom room = await Node.GetService<IChatRoom>(roomId);
            long ticket = await room.AwaitUser(new ChatUserInfo(_userId, _name));

            return new CreateRoomResponse
                {
                    Code = CreateRoomResponseCode.Ok,
                    Data = new CreateRoomResponse.CreateRoomResponseSubData { RoomId = roomId, Ticket = ticket }
                };
        }

        public async Task<long> JoinRoom(uint roomId)
        {
            IChatManager chatManager = await Node.GetService<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            if(rooms.Any(x=>x.Id == roomId))
            {
                IChatRoom room = await Node.GetService<IChatRoom>(roomId);
                long ticket = await room.AwaitUser(new ChatUserInfo(_userId, _name));
                return ticket;
            }
            return -1;
        }
    }
}
