using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core.Actors;
using Protocol.Generated;
using System.Threading.Tasks;
using MOUSE.Core;

#pragma warning disable 1998

namespace SampleServer
{
    public class UserManager : Actor, IUserManager
    {
        private uint _userCounter;
        private Dictionary<string, ChatUserInfo> _usersByName;
        private Dictionary<uint, ChatUserInfo> _usersById;


        public override void OnCreated()
        {
            _usersByName = new Dictionary<string, ChatUserInfo>();
            _usersById = new Dictionary<uint, ChatUserInfo>();
            _userCounter = 1;
        }

        [NetOperationHandler]
        public async Task<ChatUserInfo> GetUser(string name)
        {
            ChatUserInfo user;
            if (_usersByName.TryGetValue(name, out user))
                return user;

            return null;
        }

        [NetOperationHandler]
        public async Task<ChatUserInfo> TryRegisterUser(string name)
        {
            if (_usersByName.ContainsKey(name))
                return null;

            uint id = _userCounter++;

            var user = new ChatUserInfo
            {
                Id = id,
                Name = name
            };

            _usersByName.Add(name, user);
            _usersById.Add(id, user);

            return user;
        }

        [NetOperationHandler]
        public void UnregisterUser(uint id)
        {
            ChatUserInfo user;
            if (_usersById.TryGetValue(id, out user))
            {
                _usersById.Remove(id);
                _usersByName.Remove(user.Name);
            }
        }
    }
}
