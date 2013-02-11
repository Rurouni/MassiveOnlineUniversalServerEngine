using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using System.Runtime.Serialization;
using SampleC2SProtocol;

namespace SampleS2SProtocol
{
    [NetContract]
    public interface IUserManager
    {
        ChatUserInfo GetUser(string name);
        ChatUserInfo TryRegisterUser(string name);
        void UnregisterUser(uint id);
    }

    [NetContract]
    public interface IChatRoom
    {
        List<ChatUserInfo> GetUsersInside();
        long AwaitUser(ChatUserInfo user);
        bool RemoveUser(uint userId);
    }

    [DataContract]
    public class ChatUserInfo
    {
        public uint Id;
        public string Name;
    }
}
