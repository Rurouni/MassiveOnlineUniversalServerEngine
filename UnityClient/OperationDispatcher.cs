using System;
using System.Collections.Generic;
using System.Linq;
using MOUSE.Core;
using MOUSE.Core.Actors;
using NLog;

namespace MOUSE.Unity
{
    public interface IOperationDispatcher
    {
        void DispatchOneWay(object target, Message msg);
        NetProxy CreateProxy(uint serviceKey, IMessageFactory messageFactory, INetPeer peer, ActorKey? actorKey = null);

        uint GetContractId(Type contractType);
        bool TryGetContractId(Type contractType, out uint typeId);
        Type GetContractType(uint contractId);
        NetContractDescription GetContract(uint contractId);
        NetContractDescription GetContract<TNetContract>();
        NetContractDescription GetContractForMessage(uint messageId);
    }

    public class OperationDispatcher : IOperationDispatcher
    {
        Logger Log = LogManager.GetLogger("OperationDispatcher");

        private readonly Dictionary<uint, NetContractDescription> _descByTypeId = new Dictionary<uint, NetContractDescription>();
        private readonly Dictionary<uint, NetContractDescription> _descByMessageId = new Dictionary<uint, NetContractDescription>();
        private readonly Dictionary<uint, Func<IMessageFactory, object, Message, Message>> _dispatcherByMsgId = new Dictionary<uint, Func<IMessageFactory, object, Message, Message>>();
        private readonly Dictionary<Type, uint> _contractIdByContractType = new Dictionary<Type, uint>();
        private readonly IMessageFactory _messageFactory;

        public OperationDispatcher(IMessageFactory msgfactory, IEnumerable<NetProxy> importedProxies)
        {
            _messageFactory = msgfactory;

            foreach (var proxy in importedProxies)
            {
                Type type = proxy.GetType();
                if (type.ContainsAttribute<NetProxyAttribute>())
                {
                    var proxyAttr = type.GetAttribute<NetProxyAttribute>();
                    var contractAttr = proxyAttr.ContractType.GetAttribute<NetContractAttribute>();

                    var operations = new List<NetOperationDescription>();
                    foreach (var method in type.GetMethods().Where(x => x.ContainsAttribute<NetOperationDispatcherAttribute>()))
                    {
                        var opAttr = method.GetAttribute<NetOperationDispatcherAttribute>();
                        Message request = (Message)Activator.CreateInstance(opAttr.RequestMessage);
                        uint? replyId = null;
                        if (opAttr.ReplyMessage != null)
                            replyId = ((Message)Activator.CreateInstance(opAttr.ReplyMessage)).Id;
                        var opDesc = new NetOperationDescription(
                            method.Name, request.Id, replyId, (Func<IMessageFactory, object, Message, Message>)Delegate.CreateDelegate(
                                                                            typeof(Func<IMessageFactory, object, Message, Message>), method));
                        operations.Add(opDesc);
                    }

                    var desc = new NetContractDescription(proxyAttr.ContractTypeId,
                        proxyAttr.ContractType, type, contractAttr, operations);
                    RegisterService(desc);

                }
            }
        }

        public void RegisterService(NetContractDescription desc)
        {

            _descByTypeId.Add(desc.TypeId, desc);
            _contractIdByContractType.Add(desc.ContractType, desc.TypeId);

            foreach (var operation in desc.Operations)
            {
                _descByMessageId.Add(operation.RequestMessageId, desc);
                if (operation.ReplyMessageId.HasValue)
                    _descByMessageId.Add(operation.ReplyMessageId.Value, desc);
                _dispatcherByMsgId.Add(operation.RequestMessageId, operation.Dispatch);
            }

            Log.Info("Registered Entity<contractType:{0}, typeId:{1}>", desc.ContractType, desc.TypeId);
        }

        //client supports only oneWay
        public void DispatchOneWay(object service, Message msg)
        {
            Func<IMessageFactory, object, Message, Message> dispatch;
            if (_dispatcherByMsgId.TryGetValue(msg.Id, out dispatch))
                dispatch(_messageFactory, service, msg);
            else
            {
                Log.Error("Dispatcher not found for MsgId:" + msg.Id);
            }
        }


        public NetProxy CreateProxy(uint serviceKey, IMessageFactory messageFactory, INetPeer peer, ActorKey? actorKey = null)
        {
            NetProxy proxy;
            NetContractDescription desc;
            if (_descByTypeId.TryGetValue(serviceKey, out desc))
            {
                proxy = (NetProxy)Activator.CreateInstance(desc.ProxyType);
                proxy.Init(serviceKey, desc, messageFactory, peer, actorKey);
            }
            else
                throw new Exception("Unregistered entity typeId - " + serviceKey);

            return proxy;
        }

        public uint GetContractId(Type contractType)
        {
            uint typeId;
            if (_contractIdByContractType.TryGetValue(contractType, out typeId))
                return typeId;
            else
                throw new Exception("Unregistered network cotract - " + contractType.FullName);
        }

        public bool TryGetContractId(Type contractType, out uint typeId)
        {
            return _contractIdByContractType.TryGetValue(contractType, out typeId);
        }

        public Type GetContractType(uint serviceTypeId)
        {
            NetContractDescription desc;
            if (_descByTypeId.TryGetValue(serviceTypeId, out desc))
                return desc.ContractType;
            else
                return null;
        }

        public NetContractDescription GetContract(uint contractId)
        {
            NetContractDescription desc;
            if (_descByTypeId.TryGetValue(contractId, out desc))
                return desc;
            else
                return null;
        }

        public NetContractDescription GetContract<TNetContract>()
        {
            uint contractId = GetContractId(typeof(TNetContract));
            return GetContract(contractId);
        }

        public NetContractDescription GetContractForMessage(uint messageId)
        {
            NetContractDescription desc;
            if (_descByMessageId.TryGetValue(messageId, out desc))
                return desc;
            else
                return null;
        }
    }
}
