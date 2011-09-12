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

namespace MOUSE.Core
{
    public interface IEntityDomain
    {
        Task<Message> Dispatch(INodeEntity entity, Message msg);
        NodeEntityProxy CreateProxy(ulong entityId);
        [Pure]
        ulong GetFullId<TEntityContract>(uint entityId = 0) where TEntityContract : class;
        [Pure]
        uint GetTypeId(ulong entityId);
        [Pure]
        uint GetTypeId(Type contractType);
        [Pure]
        Type GetContractType(uint entityTypeId);
        [Pure]
        uint GetLocalId(ulong entityId);
        [Pure]
        NodeEntityContractDescription GetDescription(uint typeId);
        [Pure]
        NodeEntityContractDescription GetDescription(ulong entityId);
    }

    public class EntityDomain : IEntityDomain
    {
        Logger Log = LogManager.GetCurrentClassLogger();

        private Dictionary<uint, NodeEntityContractDescription> _descByTypeId = new Dictionary<uint, NodeEntityContractDescription>();
        private Dictionary<uint, Func<INodeEntity, Message, Task<Message>>> _dispatcherByMsgId = new Dictionary<uint, Func<INodeEntity, Message, Task<Message>>>();
        private Dictionary<Type, uint> _entityTypeIdByContractType = new Dictionary<Type, uint>();

        public EntityDomain(IEnumerable<NodeEntityProxy> importedProxies)
        {
            Contract.Requires(importedProxies != null);

            foreach (var proxy in importedProxies)
            {
                Type type = proxy.GetType();
                if (type.ContainsAttribute<NodeEntityProxyAttribute>())
                {
                    var proxyAttr = type.GetAttribute<NodeEntityProxyAttribute>();
                    var contractAttr = proxyAttr.ContractType.GetAttribute<NodeEntityContractAttribute>();

                    var operations = new List<NodeEntityOperationDescription>();
                    foreach (var method in type.GetMethods().Where(x => x.ContainsAttribute<NodeEntityOperationDispatcherAttribute>()))
                    {
                        var opAttr = method.GetAttribute<NodeEntityOperationDispatcherAttribute>();
                        Message request = (Message)FormatterServices.GetUninitializedObject(opAttr.RequestMessage);
                        uint? replyId = null;
                        if (opAttr.ReplyMessage != null)
                            replyId = ((Message)FormatterServices.GetUninitializedObject(opAttr.ReplyMessage)).Id;
                        var opDesc = new NodeEntityOperationDescription(
                            method.Name, request.Id, replyId, (Func<INodeEntity, Message, Task<Message>>)Delegate.CreateDelegate(
                                                                            typeof(Func<INodeEntity, Message, Task<Message>>), method));
                        operations.Add(opDesc);
                    }

                    var desc = new NodeEntityContractDescription(proxyAttr.EntityTypeId,
                        proxyAttr.ContractType, type, contractAttr, operations);
                    RegisterEntity(desc);

                }
            }
        }

        
        public void RegisterEntity(NodeEntityContractDescription desc)
        {
            Contract.Requires(desc != null);

            _descByTypeId.Add(desc.TypeId, desc);
            _entityTypeIdByContractType.Add(desc.ContractType, desc.TypeId);

            foreach (var operation in desc.Operations)
                _dispatcherByMsgId.Add(operation.RequestMessageId, operation.Dispatch);

            Log.Info("Registered Entity<contractType:{0}, typeId:{1}>", desc.ContractType, desc.TypeId);
        }

        public Task<Message> Dispatch(INodeEntity entity, Message msg)
        {
            Contract.Requires(entity != null);
            Contract.Requires(msg != null);
            Contract.Ensures(Contract.Result<Message>() != null);

            Func<INodeEntity, Message, Task<Message>> dispatch;
            if (_dispatcherByMsgId.TryGetValue(msg.Id, out dispatch))
                return dispatch(entity, msg);
            else
            {
                var tcs = new TaskCompletionSource<Message>();
                tcs.SetResult(new InvalidEntityOperation());
                return tcs.Task;
            }
            
        }

        public NodeEntityProxy CreateProxy(ulong entityId)
        {
            NodeEntityProxy proxy;
            uint typeId = GetTypeId(entityId);
            NodeEntityContractDescription desc;
            if (_descByTypeId.TryGetValue(typeId, out desc))
            {
                proxy = (NodeEntityProxy)FormatterServices.GetUninitializedObject(desc.ProxyType);
                proxy.Init(entityId, desc);
            }
            else
                throw new Exception("Unregistered entity typeId - " + typeId);
            
            return proxy;
        }

        public ulong GetFullId<TEntityContract>(uint entityId = 0) where TEntityContract : class
        {
            uint typeId;
            if (_entityTypeIdByContractType.TryGetValue(typeof(TEntityContract), out typeId))
                return ((ulong)entityId) ^ ((ulong)typeId << 32);
            else
                throw new Exception("Unregistered entity cotract - " + typeof (TEntityContract).FullName);
        }

        public uint GetTypeId(Type contractType)
        {
            uint typeId;
            if (_entityTypeIdByContractType.TryGetValue(contractType, out typeId))
                return typeId;
            else
                throw new Exception("Unregistered entity cotract - " + contractType.FullName);
        }

        public uint GetTypeId(ulong entityId)
        {
            return (uint)(entityId >> 32);
        }

        public uint GetLocalId(ulong entityId)
        {
            return (uint) (entityId & 0xffffffffUL);
        }

        public Type GetContractType(uint entityTypeId)
        {
            NodeEntityContractDescription desc;
            if (_descByTypeId.TryGetValue(entityTypeId, out desc))
                return desc.ContractType;
            else
                throw new Exception("Unregistered entity typeId - " + entityTypeId);
        }

        public NodeEntityContractDescription GetDescription(uint typeId)
        {
            NodeEntityContractDescription desc;
            if (_descByTypeId.TryGetValue(typeId, out desc))
                return desc;
            else
                throw new Exception("Unregistered entity typeId - " + typeId);
        }

        public NodeEntityContractDescription GetDescription(ulong entityId)
        {
            return GetDescription(GetTypeId(entityId));
        }
    }
}
