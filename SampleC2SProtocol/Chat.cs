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
        LoginResult Login(string name);
    }

    [NetContract]
    public interface IChatService
    {
        [NetOperation]
        List<ChatRoomInfo> GetRooms();

        [NetOperation]
        JoinRoomResponse JoinOrCreateRoom(string roomName);

        /// <returns>Ticket</returns>
        [NetOperation(InvalidRetCode = typeof(JoinRoomInvalidRetCode))]
        JoinRoomResponse JoinRoom(uint roomId);
    }
    
    [NetContract(AllowExternalConnections = true)]
    public interface IChatRoomService
    {
        [NetOperation]
        List<string> Join(long ticket);
        [NetOperation]
        void Leave();
        [NetOperation]
        void Say(string message);
    }

   
    [NetContract]
    public interface IChatRoomServiceCallback
    {
        [NetOperation]
        void OnRoomMessage(uint roomId, string message);
    }

    [DataContract]
    public enum LoginResult : byte
    {
        Ok,
        NameInUse,
        AlreadyRegistered
    }

    [DataContract]
    public enum JoinRoomInvalidRetCode
    {
        RoomNotFound,
        ClientNotAwaited
    }

    [DataContract]
    public class JoinRoomResponse
    {
        public uint RoomId;
        public long Ticket;
        public string ServerEndpoint;
    }

    [DataContract]
    public class ChatRoomInfo
    {
        public uint Id;
        public string Name;
    }

}
