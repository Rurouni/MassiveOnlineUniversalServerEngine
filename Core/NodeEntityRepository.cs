using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using NLog;

namespace MOUSE.Core
{

    public interface IEntityRepository : IEnumerable<NodeEntity>
    {
        Task<NodeEntity> Activate(ulong entityId);
        bool TryGet(ulong entityId, out NodeEntity entity);
        bool Contains(ulong entityId);
        void Remove(NodeEntity entity);
        void Add(NodeEntity entity);
        Task Delete(NodeEntity entity);
    }

    public class NodeEntityRepository : IEntityRepository
    {
        Logger Log = LogManager.GetCurrentClassLogger();
        Dictionary<ulong, NodeEntity> _entitiesByFullId = new Dictionary<ulong, NodeEntity>();
        Dictionary<uint, Type> _entityTypesByTypeId = new Dictionary<uint, Type>();

        IPersistanceProvider _storage;
        Node _node;

        public NodeEntityRepository(IPersistanceProvider storage, Node node)
        {
            _storage = storage;
            _node = node;

            MapEntitiesToContracts();
        }

        public IPersistanceProvider Storage
        {
            get { return _storage; }
        }

        public Node Node
        {
            get { return _node; }
        }

        private void MapEntitiesToContracts()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if(type.ContainsAttribute<NodeEntityAttribute>())
                    {
                        var attr = type.GetAttribute<NodeEntityAttribute>();
                        uint typeId = Node.Domain.GetTypeId(attr.ContractType);
                        _entityTypesByTypeId.Add(typeId, type);
                        Log.Info("Registered entityType:{0} for contractType:{1} with typeId:{2}", type, attr.ContractType, typeId);
                    }
                }
            }
        }

        public async Task<NodeEntity> Activate(ulong entityId)
        {
            Log.Debug("Activating entity {0}", entityId);
            NodeEntity entity;
            if (!_entitiesByFullId.TryGetValue(entityId, out entity))
            {
                NodeEntityDescription desc = Node.Domain.GetDescription(entityId);
                if (desc.Persistant)
                    entity = await Storage.Get(entityId);
                if (entity == null && desc.AutoCreate)
                    entity = Create(entityId, desc);
                if(entity != null)
                    Add(entity);
            }

            return entity;
        }

        public async Task Delete(NodeEntity entity)
        {
            Log.Debug("Deleting {0}", entity);
            if (entity.Description.Persistant)
                await Storage.Delete(entity);

            Remove(entity);
        }

        private NodeEntity Create(ulong entityId, NodeEntityDescription desc)
        {
            Log.Debug("Creating entity {0}", entityId);
            Type entityType;
            NodeEntity entity = null;
            if (_entityTypesByTypeId.TryGetValue(desc.TypeId, out entityType))
            {
                entity = (NodeEntity)FormatterServices.GetUninitializedObject(entityType);
                entity.Init(entityId, _node);
            }

            return entity;
        }
        
        public bool TryGet(ulong entityId, out NodeEntity entity)
        {
            return _entitiesByFullId.TryGetValue(entityId, out entity);
        }

        public bool Contains(ulong entityId)
        {
            return _entitiesByFullId.ContainsKey(entityId);
        }

        public void Remove(NodeEntity entity)
        {
            Log.Debug("Creating {0}", entity);
            _entitiesByFullId.Remove(entity.Id);
        }

        public void Add(NodeEntity entity)
        {
            Log.Debug("Adding {0}", entity);
            _entitiesByFullId.Add(entity.Id, entity);
        }

        public IEnumerator<NodeEntity> GetEnumerator()
        {
            return _entitiesByFullId.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
