using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;

namespace ActorChat.Gateway.Protocol
{
    [DataContract]
    public class JoinRoom : Message
    {

        [DataMember(Order = 1)]
        public string RoomName { get; set; }
        [DataMember(Order = 2)]
        public string UserName { get; set; }

    }

    [DataContract]
    public class JoinRoomResponse : Message
    {
        [DataMember(Order = 1)]
        public JoinRoomRetCode RetCode;
    }

    public enum JoinRoomRetCode
    {
        Ok = 1,
        NameIsTaken = 2,
        RoomIsFull = 3,
        Retry = 4
    }


    [DataContract]
    public class Say : Message
    {
        [DataMember(Order = 1)]
        public string Text { get; set; }
    }

    public enum SayErrors
    {
        UnknownUser = 1,
        NotConnectedToRoom = 2
    }

    [DataContract]
    public class RoomEvent : Message
    {
        [DataMember(Order = 1)]
        public string UserName { get; set; }

        [DataMember(Order = 2)]
        public string Text { get; set; }
    }
}
