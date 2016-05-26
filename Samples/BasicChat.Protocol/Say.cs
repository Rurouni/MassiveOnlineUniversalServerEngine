using System.Runtime.Serialization;
using MOUSE.Core;

namespace BasicChat.Protocol
{
    [DataContract]
    public class Say : Message
    {
        [DataMember(Order = 1)]
        public string Text { get; set; }

        //lets spice up our chat by making delivery a bit unreliable :)
        public override MessageReliability Reliability => MessageReliability.Unreliable;
    }
}