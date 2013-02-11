using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ConstrainedExecution;
using MOUSE.Core.Actors;
using NLog;
using System.Reflection;

namespace MOUSE.Core
{
    public interface IOperationDispatcher
    {
        Task<Message> Dispatch(object target, Message msg);
        void DispatchOneWay(object target, Message msg);
        NetProxy CreateProxy(uint contractId, IMessageFactory messageFactory, IOperationExecutor executor, ActorKey? actorKey = null);

        uint GetContractId(Type contractType);
        bool TryGetContractId(Type contractType, out uint typeId);
        Type GetContractType(uint contractId);
        NetContractDescription GetContract(uint contractId);
        NetContractDescription GetContract<TNetContract>();
        NetContractDescription GetContractForMessage(uint messageId);
    }

    public class OperationDispatcher : IOperationDispatcher
    {
        private readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<uint, NetContractDescription> _descByTypeId = new Dictionary<uint, NetContractDescription>();
        private readonly Dictionary<uint, NetContractDescription> _descByMessageId = new Dictionary<uint, NetContractDescription>();
        private readonly Dictionary<uint, Func<IMessageFactory, object, Message, Task<Message>>> _dispatcherByMsgId = new Dictionary<uint, Func<IMessageFactory, object, Message, Task<Message>>>();
        private readonly Dictionary<Type, uint> _contractIdByContractType = new Dictionary<Type, uint>();
        private readonly IMessageFactory _messageFactory;

        public OperationDispatcher(IMessageFactory msgfactory, IEnumerable<NetProxy> importedProxies)
        {
            Contract.Requires(importedProxies != null);
            Contract.Requires(msgfactory != null);

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
                        Message request = (Message)FormatterServices.GetUninitializedObject(opAttr.RequestMessage);
                        uint? replyId = null;
                        if (opAttr.ReplyMessage != null)
                            replyId = ((Message)FormatterServices.GetUninitializedObject(opAttr.ReplyMessage)).Id;
                        var opDesc = new NetOperationDescription(
                            method.Name, request.Id, replyId, (Func<IMessageFactory, object, Message, Task<Message>>)Delegate.CreateDelegate(
                                                                            typeof(Func<IMessageFactory, object, Message, Task<Message>>), method));
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
            Contract.Requires(desc != null);

            _descByTypeId.Add(desc.TypeId, desc);
            _contractIdByContractType.Add(desc.ContractType, desc.TypeId);

            foreach (var operation in desc.Operations)
            {
                _descByMessageId.Add(operation.RequestMessageId, desc);
                if(operation.ReplyMessageId.HasValue)
                    _descByMessageId.Add(operation.ReplyMessageId.Value, desc);
                _dispatcherByMsgId.Add(operation.RequestMessageId, operation.Dispatch);
            }

            Log.Info("Registered Entity<contractType:{0}, typeId:{1}>", desc.ContractType, desc.TypeId);
        }

        public Task<Message> Dispatch(object service, Message msg)
        {
            Contract.Requires(service != null);
            Contract.Requires(msg != null);
            Contract.Ensures(Contract.Result<Message>() != null);

            Func<IMessageFactory, object, Message, Task<Message>> dispatch;
            if (_dispatcherByMsgId.TryGetValue(msg.Id, out dispatch))
                return dispatch(_messageFactory, service, msg);
            else
            {
                var tcs = new TaskCompletionSource<Message>();
                tcs.SetResult(new InvalidOperation((ushort)BasicErrorCode.DispatcherFuncNotFound, "Dispatcher func not found for MsgId:"+msg.Id));
                return tcs.Task;
            }
        }

        public void DispatchOneWay(object service, Message msg)
        {
            Contract.Requires(service != null);
            Contract.Requires(msg != null);

            Func<IMessageFactory, object, Message, Task<Message>> dispatch;
            if (_dispatcherByMsgId.TryGetValue(msg.Id, out dispatch))
                dispatch(_messageFactory, service, msg);
        }

        public NetProxy CreateProxy(uint contractId, IMessageFactory messageFactory, IOperationExecutor executor, ActorKey? actorKey = null)
        {
            NetProxy proxy;
            NetContractDescription desc;
            if (_descByTypeId.TryGetValue(contractId, out desc))
            {
                proxy = (NetProxy)FormatterServices.GetUninitializedObject(desc.ProxyType);
                proxy.Init(desc, messageFactory, executor, actorKey);
            }
            else
                throw new Exception("Unregistered entity typeId - " + contractId);
            
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
            uint contractId = GetContractId(typeof (TNetContract));
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

    public enum BasicErrorCode
    {
        DispatcherFuncNotFound,
        NonExistentActor,
        NonAuthorized
    }
}
