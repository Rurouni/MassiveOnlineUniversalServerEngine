using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using MOUSE.Core.Actors;

namespace MOUSE.Core
{
    [Serializable]
    [DataContract]
    [KnownType(typeof(OperationHeader))]
    [KnownType(typeof(CallbackHeader))]
    [KnownType(typeof(ActorTargetHeader))]
    [KnownType(typeof(ActorDirectReplyHeader))]
    public abstract class MessageHeader {}

    [Serializable]
    [DataContract]
    public class OperationHeader : MessageHeader
    {
        [DataMember(Order = 1)]
        public readonly Guid RequestId;
        [DataMember(Order = 2)]
        public readonly Guid ActivityId;
        [DataMember(Order = 3)]
        public readonly OperationType Type;

        public OperationHeader(Guid requestId, OperationType type, Guid activityId)
        {
            RequestId = requestId;
            Type = type;
            ActivityId = activityId;
        }

        public OperationHeader(){}
    }

    public enum OperationType
    {
        OneWay = 1,
        Request = 2,
        Reply = 3
    }

    [Serializable]
    [DataContract]
    public class CallbackHeader : MessageHeader
    {
        [DataMember(Order = 1)]
        public readonly Guid CallbackChannelId;

        public CallbackHeader(Guid callbackChannelId)
        {
            CallbackChannelId = callbackChannelId;
        }

        public CallbackHeader() { }
    }


    [Serializable]
    [DataContract]
    public class ActorTargetHeader : MessageHeader
    {
        [DataMember(Order = 1)]
        public readonly ActorKey ActorKey;

        public ActorTargetHeader(ActorKey actorkey)
        {
            ActorKey = actorkey;
        }

        public ActorTargetHeader(){}
    }

    [Serializable]
    [DataContract]
    public class ActorDirectReplyHeader : MessageHeader
    {
        [DataMember(Order = 1)]
        public readonly ActorRef ActorRef;

        public ActorDirectReplyHeader(ActorRef actorRef)
        {
            ActorRef = actorRef;
        }

        public ActorDirectReplyHeader() { }
    }
}
