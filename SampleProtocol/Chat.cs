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
        [NetOperation(Lock = LockType.Full)]
        Task<LoginResult> Login(string name);
    }

    [NetContract]
    public interface IChatService
    {
        [NetOperation(Lock = LockType.ReadReentrant)]
        Task<List<ChatRoomInfo>> GetRooms();

        [NetOperation(Lock = LockType.Full)]
        Task<CreateRoomResponse> CreateRoom(string roomName);

        /// <returns>Ticket</returns>
        [NetOperation]
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
        InvalidRequest
    }

    public enum CreateRoomResponseCode
    {
        Ok,
        NameInUse,
        InvalidRequest
    }

    [DataContract]
    public class CreateRoomResponse
    {
        //aggregate optional params
        [DataContract]
        public class CreateRoomResponseSubData
        {
            [DataMember]
            public uint RoomId;
            [DataMember]
            public long Ticket;
        }

        [DataMember]
        public CreateRoomResponseCode Code;
        [DataMember]
        public CreateRoomResponseSubData Data;
    }
    
    [DataContract]
    public class ChatRoomInfo
    {
        [DataMember]
        public uint Id;
        [DataMember]
        public string Name;

        public ChatRoomInfo(uint id, string name)
        {
            Id = id;
            Name = name;
        }
    }

}
