using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Misc;

namespace MOUSE.Core.Actors
{
    public class LocalInMemoryActorSystem<TActor> : IActorSystem<TActor>
        where TActor:IActor
    {
        readonly Func<TActor> _actorFactory;
        readonly Dictionary<ActorKey, TActor> _actors = new Dictionary<ActorKey, TActor>();
        INetNode _node;


        readonly object _whole = new object();


        public LocalInMemoryActorSystem(INetNode node, Func<TActor> actorFactory)
        {
            _node = node;
            _actorFactory = actorFactory;
        }

        public void Start()
        {}

        public Task<IActorProxy> GetActor(ActorKey actorKey)
        {
            lock (_whole)
            {
                IActor actor = _actors.ContainsKey(actorKey) ? _actors[actorKey] : Spawn(actorKey);

                return Task.FromResult<IActorProxy>(new DelegatingActorProxy(actor.ActorRef, (context, msg) => actor.Process(new OperationContext(context.ReplyChannel, msg, context.ActivityId)),
                    (context, msg) => actor.Process(new OperationContext(context.ReplyChannel, msg, context.ActivityId)), Observable.Never<IActorProxy>()));
            }
        }

        public Task<IActorProxy> GetActor(ActorRef actorRef)
        {
            if (!actorRef.Location.Equals(_node.Address))
                throw new NotSupportedException("actor ref should belong to this system as it's in-memory only");

            return this.GetActor(actorRef.Key);
        }

        public Task DisposeActor(ActorKey actorKey)
        {
            lock (_whole)
            {
                TActor actor;
                if (_actors.TryGetValue(actorKey, out actor))
                {
                    _actors.Remove(actorKey);
                    //TODO: some notification
                }
                return TaskExt.Done;
            }
        }

        TActor Spawn(ActorKey actorKey)
        {
            lock (_whole)
            {

                var actor = _actorFactory();
                actor.Init(this, new ActorRef(_node.Address, actorKey));

                _actors.Add(actorKey, actor);
                return actor;
            }

        }
    }
}