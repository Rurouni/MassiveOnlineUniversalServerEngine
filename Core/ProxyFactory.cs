using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    public abstract class NodeEntityProxy
    {
        public readonly ulong EntityId;
        public readonly Node Node;

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

        public NodeEntityProxy GetProxy<TEntityContract>(ulong entityId, Node node) where TEntityContract : class 
        {
            return _prototypes[typeof(TEntityContract)].New(entityId, node);
        }
    }
}
