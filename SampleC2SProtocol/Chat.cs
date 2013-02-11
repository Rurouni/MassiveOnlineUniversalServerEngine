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
        LoginResult Login(string name);
    }

    [NetContract]
    public interface IChatService
    {
        List<string> GetRooms();
        JoinRoomResponse JoinOrCreateRoom(string roomName);
    }
    
    [NetContract(AllowExternalConnections = true, IsPrimary = false)]
    public interface IChatRoomService
    {
        //return everything said so far in room as separate strings
        [NetOperation(InvalidRetCode = typeof(JoinRoomInvalidRetCode))]
        List<string> Join(long ticket);

        void Leave();
        void Say(string message);
    }

   
    [NetContract]
    public interface IChatRoomServiceCallback
    {
        void OnRoomMessage(string roomName, string message);
    }

    [DataContract]
    public enum LoginResult : byte
    {
        Ok,
        NameInUse,
        AlreadyRegistered
    }

    [DataContract]
    public class JoinRoomResponse
    {
        public uint RoomActorId;
        public long Ticket;
        public string ServerEndpoint;
    }

    [DataContract]
    public enum JoinRoomInvalidRetCode
    {
        RoomNotFound,
        ClientNotAwaited
    }
}
