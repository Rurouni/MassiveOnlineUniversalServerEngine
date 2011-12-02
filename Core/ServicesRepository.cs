using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using NLog;

namespace MOUSE.Core
{
    public interface IServicesRepository
    {
        Task<NodeService> Activate(ServerNode node, NodeServiceKey serviceKey);
        void Deactivate(NodeService service);
        bool TryGet(NodeServiceKey serviceKey, out NodeService service);
        bool Contains(NodeServiceKey serviceKey);
        Task Delete(NodeService service);
    }

    public class ServicesRepository : IServicesRepository
    {
        public  readonly Logger Log = LogManager.GetCurrentClassLogger();
        //will be inited in ctr so we dont need thread safety here
        readonly Dictionary<uint, NodeServiceDescription> _descriptionsByTypeId = new Dictionary<uint, NodeServiceDescription>();

        //protected by lock
        readonly Dictionary<NodeServiceKey, NodeService> _services = new Dictionary<NodeServiceKey, NodeService>();

        public readonly IPersistanceProvider Storage;
        public readonly IServiceProtocol Domain;

        public ServicesRepository(IPersistanceProvider storage, IServiceProtocol domain, IEnumerable<NodeService> importedEntities)
        {
            Storage = storage;
            Domain = domain;

            foreach (var entity in importedEntities)
            {
                Type type = entity.GetType();
                var attr = type.GetAttribute<NodeServiceAttribute>();
                var contracts = new List<NodeServiceContractDescription>();
                foreach (Type netContractType in entity.GetType().GetInterfaces())
                {
                    uint typeId;
                    if(domain.TryGetContractId(netContractType, out typeId))
                        contracts.Add(Domain.GetDescription(typeId));
                }
                var serviceDesc = new NodeServiceDescription(type, contracts, attr);
                foreach (NodeServiceContractDescription netContract in serviceDesc.ImplementedContracts)
                {
                    _descriptionsByTypeId.Add(netContract.TypeId, serviceDesc);    
                }

                Log.Info("Registered Service:{0}", type, serviceDesc);
            }
        }

        public NodeServiceDescription GetDescription(uint typeId)
        {
            Contract.Ensures(Contract.Result<NodeServiceDescription>() != null);

            NodeServiceDescription desc;
            if (!_descriptionsByTypeId.TryGetValue(typeId, out desc))
                throw new Exception("No service implements net contract with TypeId:{0}");

            return desc;
        }

        public async Task<NodeService> Activate(ServerNode node, NodeServiceKey serviceKey)
        {
            Contract.Requires(serviceKey != null);
            Contract.Ensures(Contract.Result<NodeService>() != null);

            Log.Debug("Activating service {0}", serviceKey);
            lock (_services)
            {
                NodeService service;
                if (_services.TryGetValue(serviceKey, out service))
                    return service;
            }

            NodeServiceDescription desc = GetDescription(serviceKey.TypeId);
            NodeService newService = null;
            if (desc.Persistant)
                newService = await Storage.Get(serviceKey);
            if (newService == null)
                newService = CreateNew(node, serviceKey);

            return GetOrAdd(serviceKey, newService);
        }

        protected NodeService GetOrAdd(NodeServiceKey serviceKey, NodeService newService)
        {
            lock (_services)
            {
                NodeService service;
                if (_services.TryGetValue(serviceKey, out service))
                    return service;
                else
                    AddUnsafe(newService);
            }
            return newService;
        }

        protected void AddUnsafe(NodeService newService)
        {
            foreach (NodeServiceContractDescription contract in newService.Description.ImplementedContracts)
            {
                var key = new NodeServiceKey(contract.TypeId, newService.Id);
                NodeService existingService;
                if (_services.TryGetValue(key, out existingService))
                    throw new Exception(string.Format("You have intersection in inmplemented contracts between {0} and {1}",
                        existingService, newService));
                _services.Add(key, newService);
            }
        }

        protected NodeService CreateNew(ServerNode node, NodeServiceKey serviceKey)
        {
            Log.Debug("Creating service with {0}", serviceKey);

            NodeServiceDescription desc = GetDescription(serviceKey.TypeId);

            var entity = (NodeService)FormatterServices.GetUninitializedObject(desc.ServiceType);
            entity.Init(serviceKey.Id, desc, node);
            return entity;
        }

        public async Task Delete(NodeService service)
        {
            Log.Debug("Deleting {0}", service);
            Deactivate(service);

            if (service.Description.Persistant)
                await Storage.Delete(service);
        }

        public void Deactivate(NodeService service)
        {
            Log.Debug("Removing {0}", service);
            lock (_services)
            {
                foreach (NodeServiceContractDescription contract in service.Description.ImplementedContracts)
                {
                    var key = new NodeServiceKey(contract.TypeId, service.Id);
                    NodeService existingService;
                    if (!_services.ContainsKey(key))
                        throw new Exception(string.Format("Something went wrong, {0} is not fully registered in internal dictionary", service));
                    _services.Remove(key);
                }
            }
        }

        public bool TryGet(NodeServiceKey serviceKey, out NodeService service)
        {
            lock (_services)
            {
                return _services.TryGetValue(serviceKey, out service);
            }
        }

        public bool Contains(NodeServiceKey serviceKey)
        {
            lock (_services)
            {
                return _services.ContainsKey(serviceKey);
            }
        }
    }
}
