using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class NodeEntityContractAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NodeEntityOperationAttribute : Attribute
    {
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
