using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class NodeEntityContractAttribute : Attribute
    {
        public bool Persistant { get; set; }
        public bool AutoCreate { get; set; }
        public bool Connectionfull { get; set; }

        public NodeEntityContractAttribute()
        {
            Persistant = false;
            AutoCreate = true;
            Connectionfull = false;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NodeEntityOperationAttribute : Attribute
    {
        public MessagePriority Priority { get; set; }
        public MessageReliability Reliability { get; set; }

        public NodeEntityOperationAttribute()
        {
            Priority = MessagePriority.Medium;
            Reliability = MessageReliability.ReliableOrdered;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NodeEntityAttribute : Attribute
    {
        public ulong ContractType { get; set; }

        public NodeEntityAttribute(ulong contractType)
        {
            ContractType = contractType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NodeEntityProxyAttribute : Attribute
    {
        public uint EntityTypeId { get; set; }
        public Type ContractType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NodeEntityOperationDispatcher : Attribute
    {
        public Type RequestMessage { get; set; }
        public Type ReplyMessage { get; set; }
    }
}
