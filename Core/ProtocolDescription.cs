using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MOUSE.Core
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

        public bool AllowExternalConnections
        {
            get { return Attribute.AllowExternalConnections; }
        }

        public bool IsPrimary
        {
            get { return Attribute.IsPrimary; }
        }

        public override string ToString()
        {
            return string.Format("NetContract<TypeId: {0}, ContractType: {1}, ProxyType: {2}, AllowExternalConnections: {3}>", TypeId, ContractType, ProxyType, AllowExternalConnections);
        }
    }

    public class NetOperationDescription
    {
        public readonly string Name;
        public readonly uint RequestMessageId;
        public readonly uint? ReplyMessageId;
        public readonly Func<IMessageFactory, object, Message, Task<Message>> Dispatch;

        public NetOperationDescription(string name, uint requestMessageId, uint? replyMessageId, Func<IMessageFactory, object, Message, Task<Message>> dispatch)
        {
            Name = name;
            RequestMessageId = requestMessageId;
            ReplyMessageId = replyMessageId;
            Dispatch = dispatch;
        }
    }
}