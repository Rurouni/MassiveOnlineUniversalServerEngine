using System;
using System.Threading.Tasks;
using MOUSE.Core.Actors;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.Interfaces.Actors
{
    public interface IActor : IDisposable
    {
        ActorRef ActorRef { get; }
        Task<Message> Process(IOperationContext operation);
        void Init(IActorSystem system, ActorRef actorRef);
    }
}