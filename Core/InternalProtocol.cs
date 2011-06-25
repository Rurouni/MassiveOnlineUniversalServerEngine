using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MOUSE.Core
{
    public class NodeIntroductionRequest : Message
    {
        [DataMember]
        public NodeDescription SenderDescription;
    }

    public class NodeIntroductionReply
    {
        [DataMember]
        public NodeDescription ReceiverDescription;
    }

    [DataContract]
    public class NodeDescription
    {
        [DataMember]
        public ulong NodeId;
        [DataMember]
        public NodeType Type;
        [DataMember]
        public string Ip;
        [DataMember]
        public ushort Port;

        public override string ToString()
        {
            return string.Format("Node<Id:{0}, Type:{1}, Ip:{2}, Port:{3}>", NodeId, Type, Ip, Port);
        }
    }
}
