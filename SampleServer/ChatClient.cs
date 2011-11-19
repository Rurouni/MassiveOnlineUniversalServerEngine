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

        public override void Init()
        {
            SetHandler<IChatLogin>(this);
        }

        public override void OnDisconnect()
        {
            if (_state != ClientState.Unregistered)
            {
                IChatManager chatManager = Node.Get<IChatManager>();
                chatManager.UnregisterUser(_userId);
            }
        }
        
        public async Task<LoginResult> Login(string name)
        {
            if (_state != ClientState.Unregistered)
                return LoginResult.InvalidRequest;
            _name = name;
            IChatManager chatManager = Node.Get<IChatManager>();
            ChatUserInfo existingUser = await chatManager.GetUser(name);
            if (existingUser != null)
                return LoginResult.NameInUse;
            else
            {
                _userId = await chatManager.RegisterUser(name);
                _state = ClientState.Registered;
                SetHandler<IChatService>(this);
                return LoginResult.Ok;
            }
        }

        public async Task<List<ChatRoomInfo>> GetRooms()
        {
            IChatManager chatManager = Node.Get<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            return rooms;
        }

        public async Task<CreateRoomResponse> CreateRoom(string roomName)
        {
            IChatManager chatManager = Node.Get<IChatManager>();
            List<ChatRoomInfo> rooms = await chatManager.GetRooms();
            if(rooms.Where(x=>x.RoomName == roomName).Count()>0)
                return new CreateRoomResponse { Code = CreateRoomResponseCode.NameInUse };

            uint roomId = await chatManager.CreateRoom(roomName);

            IChatRoom room = Node.GetService<IChatRoom>(roomId);
            long ticket = await room.AwaitClient(_userId, _name);

            return new CreateRoomResponse
                {
                    Code = CreateRoomResponseCode.Ok,
                    Data = new CreateRoomResponse.CreateRoomResponseSubData { RoomId = roomId, Ticket = ticket }
                };
        }

        public Task<long> JoinRoom(uint roomId)
        {
            throw new NotImplementedException();
        }
    }
}
