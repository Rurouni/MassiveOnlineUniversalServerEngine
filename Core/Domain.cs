using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ConstrainedExecution;

namespace MOUSE.Core
{
    public interface IEntityDomain
    {
        void Init(Node node);
        Task<Message> Dispatch(NodeEntity entity, Message msg);
        NodeEntityProxy GetProxy(ulong entityId);
        ulong GetFullId<TEntityContract>(uint? entityId) where TEntityContract : class;
        uint GetTypeId(ulong entityId);
        Type GetContractType(uint entityTypeId);
        uint GetLocalId(ulong entityId);
        NodeEntityDescription GetDescription(uint typeId);
        NodeEntityDescription GetDescription(ulong entityId);
    }

    public class StaticDomain : IEntityDomain
    {
        private Node _node;
        private Dictionary<ulong, NodeEntityProxy> _proxyCache = new Dictionary<ulong, NodeEntityProxy>();

        private Dictionary<uint, NodeEntityDescription> _descByTypeId = new Dictionary<uint, NodeEntityDescription>();
        private Dictionary<uint, Func<NodeEntity, Message, Task<Message>>> _dispatcherByMsgId = new Dictionary<uint, Func<NodeEntity, Message, Task<Message>>>();
        private Dictionary<Type, uint> _entityTypeIdByContractType = new Dictionary<Type, uint>();

        public StaticDomain(List<NodeEntityDescription> domainDesc)
        {
            foreach (var entityDescription in domainDesc)
            {
                _descByTypeId.Add(entityDescription.TypeId, entityDescription);
                _entityTypeIdByContractType.Add(entityDescription.ContractType, entityDescription.TypeId);

                foreach (var operation in entityDescription.Operations)
                    _dispatcherByMsgId.Add(operation.RequestMessageId, operation.Dispatch);
            }
        }

        public void Init(Node node)
        {
            _node = node;
        }

        public Task<Message> Dispatch(NodeEntity entity, Message msg)
        {
            Func<NodeEntity, Message, Task<Message>> dispatch;
            if (_dispatcherByMsgId.TryGetValue(msg.Id, out dispatch))
                return dispatch(entity, msg);
            else
            {
                var tcs = new TaskCompletionSource<Message>();
                tcs.SetResult(new InvalidEntityOperation());
                return tcs.Task;
            }
        }

        public NodeEntityProxy GetProxy(ulong entityId)
        {
            NodeEntityProxy proxy;
            if (!_proxyCache.TryGetValue(entityId, out proxy))
            {
                uint typeId = GetTypeId(entityId);
                NodeEntityDescription desc;
                if (_descByTypeId.TryGetValue(typeId, out desc))
                {
                    proxy = (NodeEntityProxy)FormatterServices.GetUninitializedObject(desc.ProxyType);
                    proxy.Init(entityId, _node, desc);
                }
                else
                    throw new Exception("Unregistered entity typeId - " + typeId);
                _proxyCache.Add(entityId, proxy);
            }
            return proxy;
        }

        public ulong GetFullId<TEntityContract>(uint? entityId) where TEntityContract : class
        {
            uint typeId;
            if (_entityTypeIdByContractType.TryGetValue(typeof(TEntityContract), out typeId))
                return ((ulong)(entityId ?? 0)) ^ ((ulong)typeId << 32);
            else
                throw new Exception("Unregistered entity cotract - " + typeof (TEntityContract).FullName);
        }

        public uint GetTypeId(ulong entityId)
        {
            return (uint)entityId >> 32;
        }

        public uint GetLocalId(ulong entityId)
        {
            return (uint) (entityId & 0xffffffffUL);
        }

        public Type GetContractType(uint entityTypeId)
        {
            NodeEntityDescription desc;
            if (_descByTypeId.TryGetValue(entityTypeId, out desc))
                return desc.ContractType;
            else
                throw new Exception("Unregistered entity typeId - " + entityTypeId);
        }

        public NodeEntityDescription GetDescription(uint typeId)
        {
            NodeEntityDescription desc;
            if (_descByTypeId.TryGetValue(typeId, out desc))
                return desc;
            else
                throw new Exception("Unregistered entity typeId - " + typeId);
        }

        public NodeEntityDescription GetDescription(ulong entityId)
        {
            return GetDescription(GetTypeId(entityId));
        }


        

        
    }
}
