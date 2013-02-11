using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MOUSE.Core.Actors;
using System.Linq;

namespace MOUSE.Core.ActorCoordination
{
    /// <summary>
    /// Doesn't do any real coordination, it will just maintain local dictionary of named actors, automatically creating new one if none exists before
    /// </summary>
    public class LocalOnlyCoordinator : IActorCoordinator
    {
        private readonly object _fullLock = new object();
        private IServerNode _node;
        private uint _primaryContractId;
        private readonly Dictionary<string, Actor> _actorsByName = new Dictionary<string, Actor>();
        private readonly Dictionary<uint, Actor> _actorsByLocalId = new Dictionary<uint, Actor>();
        private readonly Subject<ActorRemoteInfo> _actorAddedEvent = new Subject<ActorRemoteInfo>();
        private readonly Subject<ActorRemoteInfo> _actorRemovedEvent = new Subject<ActorRemoteInfo>();


        public void Init(IServerNode node, uint primaryNetContractId)
        {
            _node = node;
            _primaryContractId = primaryNetContractId;
        }

        public Task<ActorKey> GetOrCreate(string name)
        {
            lock (_fullLock)
            {
                Actor actor;
                if (!_actorsByName.TryGetValue(name, out actor))
                {
                    actor = _node.Repository.Create(_node, name, _primaryContractId);
                    _actorsByName.Add(name, actor);
                    _actorsByLocalId.Add(actor.LocalId, actor);
                }

                return Task.FromResult(new ActorKey(_node.Id, actor.LocalId));
            }
        }

        public Task RemoveActor(string name)
        {
            lock (_fullLock)
            {
                Actor actor;
                if (_actorsByName.TryGetValue(name, out actor))
                {
                    _actorsByName.Remove(actor.Name);
                    _actorsByLocalId.Remove(actor.LocalId);
                }

                return Task.FromResult<object>(null);
            }
        }

        public Task<IReadOnlyList<ActorRemoteInfo>> GetActors()
        {
            lock (_fullLock)
            {
                var actors = ImmutableList<ActorRemoteInfo>.Empty;
                var actorBuilder = actors.ToBuilder();

                foreach (Actor actor in _actorsByName.Values)
                {
                    actorBuilder.Add(new ActorRemoteInfo(actor.Name, new ActorKey(_node.Id, actor.LocalId)));
                }

                return Task.FromResult((IReadOnlyList<ActorRemoteInfo>)actorBuilder.ToImmutable());
            }
        }

        public async Task<IReadOnlyList<TReply>> QueryGroup<TPrimaryNetContract, TReply>(Func<TPrimaryNetContract, Task<TReply>> funcToCall)
        {
            IReadOnlyList<ActorRemoteInfo> actors = await GetActors();
            ImmutableList<TReply>.Builder replies = ImmutableList<TReply>.Empty.ToBuilder();
            foreach (var remoteInfo in actors)
            {
                ActorProxy<TPrimaryNetContract> proxy = await _node.GetActor<TPrimaryNetContract>(remoteInfo.Key);

                TReply reply = await funcToCall(proxy.Channel);
                replies.Add(reply);
            }

            return (IReadOnlyList<TReply>)replies.ToImmutable();
        }

        public IObservable<ActorRemoteInfo> ActorAddedEvent
        {
            get { return _actorAddedEvent; }
        }

        public IObservable<ActorRemoteInfo> ActorRemovedEvent
        {
            get { return _actorRemovedEvent; }
        }
    }
}
