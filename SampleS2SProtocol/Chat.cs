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
        [NetOperation(Lock = LockType.ReadReentrant)]
        Task<ChatUserInfo> GetUser(string name);

        [NetOperation]
        Task<ChatUserInfo> TryRegisterUser(string name);

        [NetOperation]
        void UnregisterUser(uint id);

        [NetOperation(Lock = LockType.ReadReentrant)]
        Task<List<ChatRoomInfo>> GetRooms();

        [NetOperation]
        Task<uint> CreateRoom(string roomName);

        [NetOperation]
        Task DeleteRoom(uint roomId);
    }

    [NetContract]
    public interface IChatRoom
    {
        [NetOperation(Lock = LockType.ReadReentrant)]
        Task<List<ChatUserInfo>> GetUsersInside();

        [NetOperation]
        Task<long> AwaitUser(ChatUserInfo user);

        [NetOperation]
        Task RemoveUser(uint userId);
    }

    [DataContract]
    public class ChatUserInfo
    {
        [DataMember]
        public uint Id;
        [DataMember]
        public string Name;

        public ChatUserInfo(uint id, string name)
        {
            Name = name;
            Id = id;
        }
    }
}
