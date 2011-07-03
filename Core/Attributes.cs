using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNetWrapper;

namespace MOUSE.Core
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class NodeEntityContractAttribute : Attribute
    {
        public bool Persistant { get; set; }
        public bool AutoCreate { get; set; }
        public bool Connectionless { get; set; }

        public NodeEntityContractAttribute()
        {
            Persistant = false;
            AutoCreate = true;
            Connectionless = true;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NodeEntityOperationAttribute : Attribute
    {
        public MessagePriority Priority { get; set; }
        public MessageReliability Reliability { get; set; }

        public NodeEntityOperationAttribute()
        {
            Priority = MessagePriority.MEDIUM_PRIORITY;
            Reliability = MessageReliability.RELIABLE_ORDERED;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NodeEntityAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NodeEntityProxyAttribute : Attribute
    {
        public Type TargetType { get; set; }
    }
}
