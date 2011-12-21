using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using System.Runtime.Serialization;
using System.Net;

namespace SampleC2SProtocol
{
    [NetContract]
    public interface IChatLogin
    {
        [NetOperation]
        Task<LoginResult> Login(string name);
    }

    [NetContract]
    public interface IChatService
    {
        [NetOperation(Lock = LockType.ReadReentrant)]
        Task<List<ChatRoomInfo>> GetRooms();

        [NetOperation]
        Task<CreateRoomResponse> JoinOrCreateRoom(string roomName);

        /// <returns>Ticket</returns>
        [NetOperation(InvalidRetCode = typeof(JoinRoomInvalidRetCode))]
        Task<long> JoinRoom(uint roomId);
    }
    
    [NetContract(AllowExternalConnections = true)]
    public interface IChatRoomService
    {
        [NetOperation]
        Task<List<string>> Join(long ticket);

        [NetOperation(Lock = LockType.ReadReentrant)]
        void Say(string message);
    }

   
    [NetContract]
    public interface IChatRoomServiceCallback
    {
        [NetOperation]
        void OnRoomMessage(uint roomId, string message);
    }

    public enum LoginResult
    {
        Ok,
        NameInUse,
        AlreadyRegistered
    }

    public enum JoinRoomInvalidRetCode
    {
        RoomNotFound,
        ClientNotAwaited
    }

    [DataContract]
    public class CreateRoomResponse
    {
        [DataMember]
        public uint RoomId;
        [DataMember]
        public long Ticket;
    }

    [DataContract]
    public class ChatRoomInfo
    {
        [DataMember]
        public uint Id;
        [DataMember]
        public string Name;

        public ChatRoomInfo(){}

        public ChatRoomInfo(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

}
