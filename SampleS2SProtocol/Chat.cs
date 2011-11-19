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
        Task<ChatUserInfo> GetUser(string name);

        [NetOperation]
        Task<uint> RegisterUser(string name);

        [NetOperation]
        void UnregisterUser(uint id);

        [NetOperation]
        Task<List<ChatRoomInfo>> GetRooms();

        [NetOperation]
        Task<uint> CreateRoom(string roomName);

        [NetOperation]
        Task DeleteRoom(uint roomId);
    }

    [NetContract]
    public interface IChatRoom
    {
        [NetOperation]
        Task<List<ChatUserInfo>> GetUsersInside();

        [NetOperation]
        Task<List<string>> AddUser(ChatUserInfo user);

        [NetOperation]
        Task RemoveUser(uint userId);

        [NetOperation]
        void Say(uint userId, string message);
    }

    [DataContract]
    public class ChatUserInfo
    {
        [DataMember]
        public uint Id;
        [DataMember]
        public long NodeId;
        [DataMember]
        public string Name;
    }
}
