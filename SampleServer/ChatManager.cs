using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SampleS2SProtocol;
using System.Threading.Tasks;
using SampleC2SProtocol;
using MOUSE.Core;

namespace SampleServer
{
    public class ChatManager : NodeService, IChatManager
    {
        public Task<ChatUserInfo> GetUser(string name)
        {
            throw new NotImplementedException();
        }

        public Task<uint> RegisterUser(string name)
        {
            throw new NotImplementedException();
        }

        public void UnregisterUser(uint id)
        {
            throw new NotImplementedException();
        }

        public Task<List<ChatRoomInfo>> GetRooms()
        {
            throw new NotImplementedException();
        }

        public Task<uint> CreateRoom(string roomName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRoom(uint roomId)
        {
            throw new NotImplementedException();
        }
    }
}
