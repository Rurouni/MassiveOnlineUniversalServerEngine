using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core.ActorCoordination;

namespace MOUSE.Core
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class NetContractAttribute : Attribute
    {
        public bool AllowExternalConnections { get; set; }
        public bool IsPrimary { get; set; }

        public NetContractAttribute()
        {
            AllowExternalConnections = false;
            IsPrimary = true;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NetOperationAttribute : Attribute
    {
        public MessagePriority Priority { get; set; }
        public MessageReliability Reliability { get; set; }
        public LockType Lock { get; set; }


        public Type InvalidRetCode { get; set; }

        public NetOperationAttribute()
        {
            Priority = MessagePriority.Medium;
            Reliability = MessageReliability.ReliableOrdered;
            Lock = LockType.Write;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NetOperationHandlerAttribute : Attribute
    {
        public LockType Lock { get; set; }

        public NetOperationHandlerAttribute()
        {
            Lock = LockType.Write;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ActorAttribute : Attribute
    {
        public ActorAttribute()
        {
            Coordinator = typeof (IsisActorCoordinator);
        }

        public Type Coordinator { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NetProxyAttribute : Attribute
    {
        public uint ContractTypeId { get; set; }
        public Type ContractType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NetOperationDispatcherAttribute : Attribute
    {
        public Type RequestMessage { get; set; }
        public Type ReplyMessage { get; set; }
    }
}
