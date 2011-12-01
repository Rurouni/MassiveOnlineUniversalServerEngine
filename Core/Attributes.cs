using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class NetContractAttribute : Attribute
    {
        public bool AllowExternalConnections { get; set; }

        public NetContractAttribute()
        {
            AllowExternalConnections = false;
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
            Lock = LockType.Full;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NodeServiceAttribute : Attribute
    {
        public bool Persistant { get; set; }
        public bool AutoCreate { get; set; }

        public NodeServiceAttribute()
        {
            Persistant = false;
            AutoCreate = true;
        }
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
