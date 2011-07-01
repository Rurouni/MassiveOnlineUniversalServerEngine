using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    public interface IEntityDomain
    {
        void Init(Node node);
        Message Deserialize(uint messageId, NativeReader reader);
        Task<Message> Dispatch(NodeEntity entity, Message msg);
        NodeEntityProxy GetProxy(ulong entityId);
        NodeEntity Create(ulong entityId);
        ulong GetEntityFullId<TEntityContract>(uint? entityId) where TEntityContract : class;
        Type GetEntityType(ulong entityId);
        
    }

    public class NodeEntityDescription
    {
        public uint TypeId;
        public Type ContractType;
        public List<NodeEntityOperationDescription> Operations;
        public Func<Node, >
        
    }

    public class NodeEntityOperationDescription
    {
        public NodeEntityDescription Owner;
        public Func<uint, NativeReader, Message> Deserialize;
    }

    public class ExtensibleDomain : IEntityDomain
    {
        private Node _node;
        private Dictionary<ulong, NodeEntityProxy> _proxyCache = new Dictionary<ulong, NodeEntityProxy>();

        private Dictionary<uint, Func<uint, NativeReader, Message>> _creatorsByMsgId = new Dictionary<uint, Func<uint, NativeReader, Message>>();
        
        public void Init(Node node)
        {
            _node = node;
        }

        public Message Deserialize(uint messageId, NativeReader reader)
        {
            Func<uint, NativeReader, Message> creator;
            if (_creatorsByMsgId.TryGetValue(messageId, out creator))
                return creator(messageId, reader);
            else
                return null;
        }

        public Task<Message> Dispatch(NodeEntity entity, Message msg)
        {
            _operationDescriptions.TryGet(msg.Id)
        }

        public NodeEntityProxy GetProxy(ulong entityId)
        {
            NodeEntityProxy proxy;
            if (!_proxyCache.TryGetValue(entityId, out proxy))
            {
                Type type = GetEntityType(entityId);
                if(!_proxyPrototypes.ContainsKey(type))
                    throw new Exception(type.ToString()+" has no proxy mapping");
                proxy = _proxyPrototypes[type].New(entityId, _node);
                _proxyCache.Add(entityId, proxy);
            }
            return proxy;
        }

        public NodeEntity Create(ulong entityId)
        {
            throw new NotImplementedException();
        }

        public ulong GetEntityFullId<TEntityContract>(uint? entityId) where TEntityContract : class
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(ulong entityId)
        {
            throw new NotImplementedException();
        }
    }
}
