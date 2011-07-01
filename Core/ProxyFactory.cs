using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNetWrapper;

namespace MOUSE.Core
{
    public abstract class NodeEntityProxy
    {
        public readonly ulong EntityId;
        public readonly Node Node;
        public ulong? CachedNodeId;

        public NodeEntityProxy(ulong entityId, Node node)
        {
            EntityId = entityId;
            Node = node;
        }

        public abstract NodeEntityProxy New(ulong entityId, Node node);
    }

    public class ProxyFactory
    {
        private Dictionary<Type, NodeEntityProxy> _prototypes = new Dictionary<Type, NodeEntityProxy>();
        private Dictionary<ulong, NodeEntityProxy> _cache = new Dictionary<ulong, NodeEntityProxy>();
        
        public ProxyFactory(Node node)
        {
            _node = node;
        }

        public NodeEntityProxy GetProxy(ulong entityId)
        {
            NodeEntityProxy proxy;
            if (!_cache.TryGetValue(entityId, out proxy))
            {
                Type type = _node.Domain.GetEntityType(entityId);
                if(!_prototypes.ContainsKey(type))
                    throw new Exception(type.ToString()+" has no proxy mapping");
                proxy = _prototypes[type].New(entityId, _node);
                _cache.Add(entityId, proxy);
            }
            return proxy;
        }
    }
}
