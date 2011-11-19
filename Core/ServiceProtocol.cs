using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ConstrainedExecution;
using NLog;
using System.ComponentModel.Composition;
using System.Reflection;

namespace MOUSE.Core
{
    public interface IServiceProtocol
    {
        Task<Message> Dispatch(object target, Message msg);
        NodeServiceProxy CreateProxy(ulong serviceId);
        [Pure]
        ulong GetFullId<TServiceContract>(uint serviceId = 0);
        [Pure]
        uint GetContractId(ulong serviceId);
        [Pure]
        uint GetContractId(Type contractType);
        [Pure]
        Type GetContractType(uint serviceTypeId);
        [Pure]
        uint GetLocalId(ulong serviceId);
        [Pure]
        NodeServiceContractDescription GetDescription(uint serviceTypeId);
        [Pure]
        NodeServiceContractDescription GetDescription(ulong serviceId);
    }

    public class ServiceProtocol : IServiceProtocol
    {
        Logger Log = LogManager.GetCurrentClassLogger();

        private Dictionary<uint, NodeServiceContractDescription> _descByTypeId = new Dictionary<uint, NodeServiceContractDescription>();
        private Dictionary<uint, Func<IMessageFactory, object, Message, Task<Message>>> _dispatcherByMsgId = new Dictionary<uint, Func<IMessageFactory, object, Message, Task<Message>>>();
        private Dictionary<Type, uint> _serviceTypeIdByContractType = new Dictionary<Type, uint>();
        private IMessageFactory _messageFactory;

        public ServiceProtocol(IMessageFactory msgfactory, IEnumerable<NodeServiceProxy> importedProxies)
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

                    var operations = new List<NodeServiceOperationDescription>();
                    foreach (var method in type.GetMethods().Where(x => x.ContainsAttribute<NetOperationDispatcherAttribute>()))
                    {
                        var opAttr = method.GetAttribute<NetOperationDispatcherAttribute>();
                        Message request = (Message)FormatterServices.GetUninitializedObject(opAttr.RequestMessage);
                        uint? replyId = null;
                        if (opAttr.ReplyMessage != null)
                            replyId = ((Message)FormatterServices.GetUninitializedObject(opAttr.ReplyMessage)).Id;
                        var opDesc = new NodeServiceOperationDescription(
                            method.Name, request.Id, replyId, (Func<IMessageFactory, object, Message, Task<Message>>)Delegate.CreateDelegate(
                                                                            typeof(Func<IMessageFactory, object, Message, Task<Message>>), method));
                        operations.Add(opDesc);
                    }

                    var desc = new NodeServiceContractDescription(proxyAttr.ContractTypeId,
                        proxyAttr.ContractType, type, contractAttr, operations);
                    RegisterService(desc);

                }
            }
        }

        
        public void RegisterService(NodeServiceContractDescription desc)
        {
            Contract.Requires(desc != null);

            _descByTypeId.Add(desc.TypeId, desc);
            _serviceTypeIdByContractType.Add(desc.ContractType, desc.TypeId);

            foreach (var operation in desc.Operations)
                _dispatcherByMsgId.Add(operation.RequestMessageId, operation.Dispatch);

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
                tcs.SetResult(new InvalidOperation((ushort)BasicErrorCode.DispatcherNotFound, "Dispatcher not found for MsgId:"+msg.Id));
                return tcs.Task;
            }
            
        }

        public NodeServiceProxy CreateProxy(ulong serviceId)
        {
            NodeServiceProxy proxy;
            uint typeId = GetContractId(serviceId);
            NodeServiceContractDescription desc;
            if (_descByTypeId.TryGetValue(typeId, out desc))
            {
                proxy = (NodeServiceProxy)FormatterServices.GetUninitializedObject(desc.ProxyType);
                proxy.Init(serviceId, desc);
            }
            else
                throw new Exception("Unregistered entity typeId - " + typeId);
            
            return proxy;
        }

        public ulong GetFullId<ServiceContract>(uint entityId = 0)
        {
            uint typeId;
            if (_serviceTypeIdByContractType.TryGetValue(typeof(ServiceContract), out typeId))
                return ((ulong)entityId) ^ ((ulong)typeId << 32);
            else
                throw new Exception("Unregistered service cotract - " + typeof(ServiceContract).FullName);
        }

        public uint GetContractId(Type contractType)
        {
            uint typeId;
            if (_serviceTypeIdByContractType.TryGetValue(contractType, out typeId))
                return typeId;
            else
                throw new Exception("Unregistered entity cotract - " + contractType.FullName);
        }

        public uint GetContractId(ulong serviceId)
        {
            return (uint)(serviceId >> 32);
        }

        public uint GetLocalId(ulong serviceId)
        {
            return (uint) (serviceId & 0xffffffffUL);
        }

        public Type GetContractType(uint serviceTypeId)
        {
            NodeServiceContractDescription desc;
            if (_descByTypeId.TryGetValue(serviceTypeId, out desc))
                return desc.ContractType;
            else
                throw new Exception("Unregistered service typeId - " + serviceTypeId);
        }

        public NodeServiceContractDescription GetDescription(uint typeId)
        {
            NodeServiceContractDescription desc;
            if (_descByTypeId.TryGetValue(typeId, out desc))
                return desc;
            else
                throw new Exception("Unregistered service typeId - " + typeId);
        }

        public NodeServiceContractDescription GetDescription(ulong serviceId)
        {
            return GetDescription(GetContractId(serviceId));
        }
    }

    public enum BasicErrorCode
    {
        DispatcherNotFound
    }
}
