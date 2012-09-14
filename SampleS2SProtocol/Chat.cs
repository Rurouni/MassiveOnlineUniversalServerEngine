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
    public interface IChatManager
    {
        [NetOperation]
        ChatUserInfo GetUser(string name);

        [NetOperation]
        ChatUserInfo TryRegisterUser(string name);

        [NetOperation]
        void UnregisterUser(uint id);

        [NetOperation]
        List<ChatRoomInfo> GetRooms();

        [NetOperation]
        uint GetOrCreateRoom(string roomName);

        [NetOperation]
        bool DeleteRoom(uint roomId);
    }

    [NetContract]
    public interface IChatRoom
    {
        [NetOperation]
        List<ChatUserInfo> GetUsersInside();

        [NetOperation]
        long AwaitUser(ChatUserInfo user);

        [NetOperation]
        bool RemoveUser(uint userId);
    }

    [DataContract]
    public class ChatUserInfo
    {
        [DataMember]
        public uint Id;
        [DataMember]
        public string Name;
    }
}
