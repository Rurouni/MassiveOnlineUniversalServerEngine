using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;

namespace MOUSE.Unity
{
    public class NetContractDescription
    {
        public readonly uint TypeId;
        public readonly Type ContractType;
        public readonly Type ProxyType;
        public readonly List<NetOperationDescription> Operations;
        public readonly NetContractAttribute Attribute;

        public NetContractDescription(uint typeId, Type contractType, Type proxyType,
            NetContractAttribute contractAttribute, List<NetOperationDescription> operations)
        {
            TypeId = typeId;
            ContractType = contractType;
            ProxyType = proxyType;
            Attribute = contractAttribute;
            Operations = operations;
        }
    }

    public class NetOperationDescription
    {
        public readonly string Name;
        public readonly uint RequestMessageId;
        public readonly uint? ReplyMessageId;
        public readonly Func<IMessageFactory, object, Message, Message> Dispatch;

        public NetOperationDescription(string name, uint requestMessageId, uint? replyMessageId, Func<IMessageFactory, object, Message, Message> dispatch)
        {
            Name = name;
            RequestMessageId = requestMessageId;
            ReplyMessageId = replyMessageId;
            Dispatch = dispatch;
        }
    }
}
