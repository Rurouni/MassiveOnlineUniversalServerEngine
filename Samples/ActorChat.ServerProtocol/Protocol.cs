using System.Runtime.Serialization;
using MOUSE.Core;
using MOUSE.Core.Interfaces.Actors;

namespace ActorChat.Backend.Protocol
{
    public interface IRoomActor : IActor {}

    [DataContract]
    public class JoinRoomS2S : Message
    {
        [DataMember(Order = 1)]
        public string RoomName { get; set; }
        [DataMember(Order = 2)]
        public string UserName { get; set; }

        [DataMember(Order = 3)]
        public CallbackChannelRef CallbackChannelRef { get; set; }

    }

    [DataContract]
    public class SayS2S : Message
    {
        [DataMember(Order = 1)]
        public string Text { get; set; }

        [DataMember(Order = 2)]
        public string UserName { get; set; }
    }
}
