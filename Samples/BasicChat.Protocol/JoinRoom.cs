using System.Runtime.Serialization;
using MOUSE.Core;

namespace BasicChat.Protocol
{
    [DataContract]
    public class JoinRoom : Message
    {
        [DataMember(Order = 1)]
        public string RoomName { get; set; }
        [DataMember(Order = 2)]
        public string ClientName { get; set; }
    }

    [DataContract]
    public class JoinRoomResponse : Message
    {
        [DataMember(Order = 1)]
        public JoinRoomRetCode RetCode { get; set; }
    }

    public enum JoinRoomRetCode
    {
        Ok = 1,
        NameIsTaken = 2
    }
}