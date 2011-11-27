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
    public interface IServiceRepository : IEnumerable<NodeService>
    {
        Task<NodeService> Activate(ulong entityId);
        bool TryGet(ulong entityId, out NodeService entity);
        bool Contains(ulong entityId);
        void Remove(NodeService entity);
        void Add(NodeService entity);
        Task<NodeService> Create(ulong entityId);
        Task<TEntity> Create<TEntity>(uint localEntityId = 0) where TEntity : NodeService;
        Task Delete(NodeService entity);
    }

    public class ServiceRepository : IServiceRepository
    {
        Logger Log = LogManager.GetCurrentClassLogger();
        Dictionary<uint, NodeServiceDescription> _descriptionsByTypeId = new Dictionary<uint, NodeServiceDescription>();
        Dictionary<ulong, NodeService> _entitiesByFullId = new Dictionary<ulong, NodeService>();

        public readonly IPersistanceProvider Storage;
        public readonly IServiceProtocol Domain;

        public ServiceRepository(IPersistanceProvider storage, IServiceProtocol domain, IEnumerable<NodeService> importedEntities)
        {
            Storage = storage;
            Domain = domain;

            foreach (var entity in importedEntities)
            {
                Type type = entity.GetType();
                var attr = type.GetAttribute<NodeEntityAttribute>();
                uint typeId = Domain.GetContractId(attr.ContractType);
                var desc = new NodeServiceDescription(type, Domain.GetDescription(typeId), attr);
                _descriptionsByTypeId.Add(typeId, desc);
                Log.Info("Registered entityType:{0} for contractType:{1} with typeId:{2}", type, attr.ContractType, typeId);
            }
        }

        public NodeServiceDescription GetDescription(ulong entityId)
        {
            NodeServiceDescription desc;
            uint typeId = Domain.GetContractId(entityId);
            if (!_descriptionsByTypeId.TryGetValue(typeId, out desc))
                throw new Exception("Entity with TypeId:{0} is not implemented");

            return desc;
        }

        public async Task<NodeService> Activate(ulong entityId)
        {
            Log.Debug("Activating entity {0}", entityId);
            NodeService entity;
            if (!_entitiesByFullId.TryGetValue(entityId, out entity))
            {
                NodeServiceDescription desc = GetDescription(entityId);
                if (desc.Persistant)
                    entity = await Storage.Get(entityId);
                if (entity == null && desc.AutoCreate)
                    entity = Create(entityId, desc);
                if(entity != null)
                    Add(entity);
            }

            return entity;
        }

        public async Task<NodeService> Create(ulong entityId)
        {
            Log.Debug("Creating entity {0}", entityId);

            NodeService entity;
            if (_entitiesByFullId.TryGetValue(entityId, out entity))
                throw new EntityAlreadyExistException(entity);

            NodeServiceDescription desc = GetDescription(entityId);
            if (desc.Persistant)
            {
                entity = await
                Storage.Get(entityId);
                if (entity != null)
                    throw new EntityAlreadyExistException(entity);
            }

            entity = Create(entityId, desc);
            Add(entity);

            return entity;
        }

        public async Task<TEntity> Create<TEntity>(uint localEntityId = 0) where TEntity : NodeService
        {
            NodeService entity = await Create(Domain.GetFullId<TEntity>(localEntityId));
            return (TEntity)entity;
        }

        public async Task Delete(NodeService entity)
        {
            Log.Debug("Deleting {0}", entity);
            if (entity.Description.Persistant)
                await Storage.Delete(entity);

            Remove(entity);
        }

        private NodeService Create(ulong entityId, NodeServiceDescription desc)
        {
            Log.Debug("Creating entity {0}", entityId);
            var entity = (NodeService)FormatterServices.GetUninitializedObject(desc.ImplementerType);
            entity.Init(entityId, desc);
            return entity;
        }
        
        public bool TryGet(ulong entityId, out NodeService entity)
        {
            return _entitiesByFullId.TryGetValue(entityId, out entity);
        }

        public bool Contains(ulong entityId)
        {
            return _entitiesByFullId.ContainsKey(entityId);
        }

        public void Remove(NodeService entity)
        {
            Log.Debug("Creating {0}", entity);
            _entitiesByFullId.Remove(entity.FullId);
        }

        public void Add(NodeService entity)
        {
            Log.Debug("Adding {0}", entity);
            _entitiesByFullId.Add(entity.FullId, entity);
        }

        public IEnumerator<NodeService> GetEnumerator()
        {
            return _entitiesByFullId.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class EntityAlreadyExistException : Exception
    {
        public NodeService Entity;

        public EntityAlreadyExistException(NodeService entity)
        {
            Entity = entity;
        }
    }

    public class EntityNotFoundException : Exception
    {
        public ulong EntityId;

        public EntityNotFoundException(ulong entityId)
        {
            EntityId = entityId;
        }
    }
}
