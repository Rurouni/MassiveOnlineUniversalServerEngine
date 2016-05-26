using System;
using System.Threading.Tasks;
using MOUSE.Core.Actors;

namespace MOUSE.Core.Interfaces.Actors
{
    public interface IActorSystem
    {
        void Start();
        
        Task<IActorProxy> GetActor(ActorKey actorId);
        Task<IActorProxy> GetActor(ActorRef actorRef);

        Task DisposeActor(ActorKey actorKey);
    }

    public interface IActorSystem<TActor> : IActorSystem
        where TActor : IActor
    {
    }
}