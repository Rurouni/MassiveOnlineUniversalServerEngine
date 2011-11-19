using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using SampleC2SProtocol;
using SampleS2SProtocol;

namespace SampleServer
{
    public class ChatRoom : NodeService, IChatRoom, IChatRoomService
    {
        private long _ticketCounter = 0;
        readonly List<ChatUserInfo> _users = new List<ChatUserInfo>();
        
        readonly Dictionary<long, ChatUserInfo> _awaitingUsers = new Dictionary<long, ChatUserInfo>();

        public async Task<List<ChatUserInfo>> GetUsersInside()
        {
            return _users;
        }

        public async Task<long> AwaitUser(ChatUserInfo user)
        {
            var ticket = _ticketCounter++;
            _awaitingUsers.Add(ticket, user);
        }

        public async Task RemoveUser(uint userId)
        {
            throw new NotImplementedException();
        }




        public async Task<List<string>> Join(long ticket)
        {
            throw new NotImplementedException();
        }

        public async void Say(string message)
        {
            throw new NotImplementedException();
        }
    }
}
