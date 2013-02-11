using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MOUSE.Core.Actors;

namespace MOUSE.Core.ActorCoordination
{
    public interface IActorCoordinator
    {
        void Init(IServerNode node, uint primaryNetContractId);

        /// <summary>
        /// Decision where to create is on coordinator
        /// </summary>
        Task<ActorKey> GetOrCreate(string name);
        Task RemoveActor(string name);

        Task<IReadOnlyList<ActorRemoteInfo>> GetActors();
        Task<IReadOnlyList<TReply>> QueryGroup<TNetContract, TReply>(Func<TNetContract, Task<TReply>> funcToCall);

        IObservable<ActorRemoteInfo> ActorAddedEvent { get; }
        IObservable<ActorRemoteInfo> ActorRemovedEvent { get; }
    }
}