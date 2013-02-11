using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using NLog;

namespace MOUSE.Core.Actors
{
    public interface IActorRepository
    {
        Actor Create(IServerNode node, string name, uint primaryContractId);
        void Remove(uint actorLocalId);
        bool TryGet(uint actorLocalId, out Actor actor);
        bool Contains(uint actorLocalId);
        bool Remove(uint primaryNetContractId, string actorName);
        IEnumerable<ActorDescription> ActorDescriptions { get; }
    }

    public class ActorRepository : IActorRepository
    {
        private int _idCounter = 0;
        public  readonly Logger Log = LogManager.GetCurrentClassLogger();
        //will be inited in ctr so we dont need thread safety here
        readonly Dictionary<uint, ActorDescription> _descriptionsByTypeId = new Dictionary<uint, ActorDescription>();

        private readonly object _fullLock = new object();
        readonly Dictionary<uint, Actor> _actorsByLocalId = new Dictionary<uint, Actor>();

        public readonly IOperationDispatcher Dispatcher;

        public ActorRepository(IOperationDispatcher dispatcher, IEnumerable<Actor> actorPrototypes)
        {
            Dispatcher = dispatcher;

            foreach (var entity in actorPrototypes)
            {
                Type type = entity.GetType();
                var attr = type.GetAttribute<ActorAttribute>();
                var contracts = new List<NetContractDescription>();
                foreach (Type netContractType in entity.GetType().GetInterfaces())
                {
                    uint typeId;
                    if(dispatcher.TryGetContractId(netContractType, out typeId))
                        contracts.Add(Dispatcher.GetContract(typeId));
                }
                var actorDescription = new ActorDescription(type, contracts, attr);
                _descriptionsByTypeId.Add(actorDescription.PrimaryContract.TypeId, actorDescription);

                Log.Info("Registered {0}", type, actorDescription);
            }
        }

        public ActorDescription GetDescription(uint primaryContractId)
        {
            Contract.Ensures(Contract.Result<ActorDescription>() != null);

            ActorDescription desc;
            if (!_descriptionsByTypeId.TryGetValue(primaryContractId, out desc))
                throw new Exception("No actor type registered with primary net contract of TypeId:{0}");

            return desc;
        }

        public Actor Create(IServerNode node, string name, uint primaryContractId)
        {
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<Actor>() != null);

            ActorDescription desc = GetDescription(primaryContractId);

            Log.Debug("Creating {0} with name:{1}", desc, name);

            var actor = (Actor)Activator.CreateInstance(desc.Type);
            uint localId = (uint)Interlocked.Increment(ref _idCounter);
            actor.Init(name, localId, desc, node);

            lock (_fullLock)
            {
                _actorsByLocalId.Add(actor.LocalId, actor);
            }

            return actor;
        }

        public void Remove(uint actorLocalId)
        {
            Log.Debug("Removing Actor<Id:{0}>", actorLocalId);
            lock (_fullLock)
            {
                _actorsByLocalId.Remove(actorLocalId);
            }
        }

        public bool TryGet(uint localActorId, out Actor actor)
        {
            lock (_fullLock)
            {
                return _actorsByLocalId.TryGetValue(localActorId, out actor);
            }
        }

        public bool Contains(uint localActorId)
        {
            lock (_fullLock)
            {
                return _actorsByLocalId.ContainsKey(localActorId);
            }
        }


        public bool Remove(uint primaryNetContractId, string actorName)
        {
            lock (_fullLock)
            {
                uint? actorId = null;
                foreach (Actor actor in _actorsByLocalId.Values)
                {
                    if (actor.Name == actorName && actor.Description.PrimaryContract.TypeId == primaryNetContractId)
                    {
                        actorId = actor.LocalId;
                        break;
                    }
                }
                if (actorId.HasValue)
                {
                    _actorsByLocalId.Remove(actorId.Value);
                }
                return actorId.HasValue;
            }
        }


        public IEnumerable<ActorDescription> ActorDescriptions 
        {
            get { return _descriptionsByTypeId.Values; }
        }
    }
}
