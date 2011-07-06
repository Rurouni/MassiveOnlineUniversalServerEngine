using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOUSE.Core
{
    public interface IEntityDomain
    {
        void Init(Node node);
        Message Deserialize(uint messageId, NativeReader reader);
        Task<Message> Dispatch(NodeEntity entity, Message msg);
        NodeEntityProxy GetProxy(ulong entityId);
        NodeEntity Create(ulong entityId);
        ulong GetFullId<TEntityContract>(uint? entityId) where TEntityContract : class;
        uint GetTypeId(ulong entityId);
        Type GetContractType(uint entityTypeId);
        uint GetLocalId(ulong entityId);
        NodeEntityDescription GetDescription(uint typeId);
    }

    public class NodeEntityDescription
    {
        public uint TypeId;
        public Type ContractType;
        public Dictionary<uint, NodeEntityOperationDescription> OperationByMsgId;
        public Func<ulong, Node, NodeEntity> NewEntity;
        public Func<ulong, Node, NodeEntityProxy> NewProxy;
        public NodeEntityContractAttribute ContractAttribute;

    }

    public class NodeEntityOperationDescription
    {
        public NodeEntityDescription Owner;
        public Func<uint, NativeReader, Message> Deserialize;
        public NodeEntityOperationAttribute Attribute;
    }

    public class BaseDomain : IEntityDomain
    {
        private Node _node;
        private Dictionary<ulong, NodeEntityProxy> _proxyCache = new Dictionary<ulong, NodeEntityProxy>();

        private Dictionary<uint, NodeEntityDescription> _descByTypeId = new Dictionary<uint, NodeEntityDescription>();
        private Dictionary<uint, Func<NativeReader, Message>> _deserialzerByMsgId = new Dictionary<uint, Func<NativeReader, Message>>();
        private Dictionary<uint, Func<NodeEntity, Message, Task<Message>>> _dispatcherByMsgId = new Dictionary<uint, Func<NodeEntity, Message, Task<Message>>>();
        private Dictionary<Type, uint> _entityTypeIdByContractType = new Dictionary<Type, uint>();
        
        public void Init(Node node)
        {
            _node = node;

            _deserialzerByMsgId.Add((uint)NodeMessageId.Empty, (reader) => new EmptyMessage(reader));
            _deserialzerByMsgId.Add((uint)NodeMessageId.ConnectionReply, (reader) => new ConnectReply(reader));
            _deserialzerByMsgId.Add((uint)NodeMessageId.ConnectionRequest, (reader) => new ConnectRequest(reader));
            _deserialzerByMsgId.Add((uint)NodeMessageId.EntityDiscoveryReply, (reader) => new EntityDiscoveryRequest(reader));
            _deserialzerByMsgId.Add((uint)NodeMessageId.EntityDiscoveryRequest, (reader) => new EntityDiscoveryRequest(reader));
            _deserialzerByMsgId.Add((uint)NodeMessageId.InvalidEntityOperation, (reader) => new InvalidEntityOperation(reader));
            _deserialzerByMsgId.Add((uint)NodeMessageId.UpdateClusterInfo, (reader) => new UpdateClusterInfo(reader));

        }

        public Message Deserialize(uint messageId, NativeReader reader)
        {
            Func<uint, NativeReader, Message> deserialize;
            if (_deserialzerByMsgId.TryGetValue(messageId, out deserialize))
                return deserialize(reader);
            else
                return null;
        }

        public Task<Message> Dispatch(NodeEntity entity, Message msg)
        {
            Func<NodeEntity, Message, Task<Message>> dispatch;
            if (_dispatcherByMsgId.TryGetValue(messageId, out dispatch))
                return dispatch(entity, msg);
            else
                return new InvalidEntityOperation();
        }

        public NodeEntityProxy GetProxy(ulong entityId)
        {
            NodeEntityProxy proxy;
            if (!_proxyCache.TryGetValue(entityId, out proxy))
            {
                uint typeId = GetEntityTypeId(entityId);
                NodeEntityDescription desc;
                if (_descByTypeId.TryGet(typeId, out desc))
                    proxy = desc.NewProxy(entityId, _node);
                else
                    throw new Exception("Unregistered entity typeId - " + typeId);
                _proxyCache.Add(entityId, proxy);
            }
            return proxy;
        }

        public NodeEntity Create(ulong entityId)
        {
            uint typeId = GetEntityTypeId(entityId);
            NodeEntityDescription desc;
            if (_descByTypeId.TryGet(typeId, out desc))
                return desc.NewEntity(entityId, _node);
            else
                throw new Exception("Unregistered entity typeId - " + typeId);
        }

        public ulong GetFullId<TEntityContract>(uint? entityId) where TEntityContract : class
        {
            uint typeId;
            if (_entityTypeIdByContractType.TryGet(typeof(TEntityContract), out typeId))
                return ((ulong)entityId ?? 0UL) ^ ((ulong)typeId << 32);
            else
                throw new Exception("Unregistered entity cotract - " + typeof (TEntityContract).FullName);
        }

        public uint GetTypeId(ulong entityId)
        {
            return entityId >> 32;
        }

        public uint GetLocalId(ulong entityId)
        {
            return (uint) (entityId & 0xffffffff);
        }

        public Type GetContractType(uint entityTypeId)
        {
            NodeEntityDescription desc;
            if (_descByTypeId.TryGet(entityTypeId, out desc))
                return desc.ContractType;
            else
                throw new Exception("Unregistered entity typeId - " + entityTypeId);
        }
    }
}
