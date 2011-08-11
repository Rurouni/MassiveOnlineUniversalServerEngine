using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

    public class EntityRepository : IEntityRepository
    {
        Logger Log = LogManager.GetCurrentClassLogger();
        Dictionary<uint, NodeEntityDescription> _descriptionsByTypeId = new Dictionary<uint, NodeEntityDescription>();
        Dictionary<ulong, NodeEntity> _entitiesByFullId = new Dictionary<ulong, NodeEntity>();

        public readonly IPersistanceProvider Storage;
        public readonly IEntityDomain Domain;

        public EntityRepository(IPersistanceProvider storage, IEntityDomain domain, IEnumerable<NodeEntity> importedEntities)
        {
            Storage = storage;
            Domain = domain;

            foreach (var entity in importedEntities)
            {
                Type type = entity.GetType();
                var attr = type.GetAttribute<NodeEntityAttribute>();
                uint typeId = Domain.GetTypeId(attr.ContractType);
                var desc = new NodeEntityDescription(type, Domain.GetDescription(typeId), attr);
                _descriptionsByTypeId.Add(typeId, desc);
                Log.Info("Registered entityType:{0} for contractType:{1} with typeId:{2}", type, attr.ContractType, typeId);
            }
        }

        public NodeEntityDescription GetDescription(ulong entityId)
        {
            NodeEntityDescription desc;
            uint typeId = Domain.GetTypeId(entityId);
            if (!_descriptionsByTypeId.TryGetValue(typeId, out desc))
                throw new Exception("Entity with TypeId:{0} is not implemented");

            return desc;
        }

        public async Task<NodeEntity> Activate(ulong entityId)
        {
            Log.Debug("Activating entity {0}", entityId);
            NodeEntity entity;
            if (!_entitiesByFullId.TryGetValue(entityId, out entity))
            {
                NodeEntityDescription desc = GetDescription(entityId);
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
            var entity = (NodeEntity)FormatterServices.GetUninitializedObject(desc.ImplementerType);
            entity.Init(entityId, desc);
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
