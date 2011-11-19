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
        [NetOperation]
        Task<List<ChatRoomInfo>> GetRooms();

        [NetOperation]
        Task<CreateRoomResponse> CreateRoom(string roomName);

        /// <returns>Ticket</returns>
        [NetOperation]
        Task<long> JoinRoom(uint roomId);
    }
    
    [NetContract]
    public interface IChatRoomService
    {
        [NetOperation]
        Task<List<string>> Join(long ticket);

        [NetOperation]
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
        public uint RoomId;
        [DataMember]
        public string RoomName;
    }

}
