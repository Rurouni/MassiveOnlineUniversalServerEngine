using System.Runtime.Serialization;
using MOUSE.Core;

namespace BasicChat.Protocol
{
    [DataContract]
    public class RoomEvent : Message
    {
        [DataMember(Order = 1)]
        public string UserName { get; set; }

        [DataMember(Order = 2)]
        public string Text { get; set; }
    }
}
