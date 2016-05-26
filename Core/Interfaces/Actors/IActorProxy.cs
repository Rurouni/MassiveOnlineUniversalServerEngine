using System;
using System.Threading.Tasks;
using MOUSE.Core.Actors;

namespace MOUSE.Core.Interfaces.Actors
{
    public interface IActorProxy
    {
        ActorRef ActorRef { get; }
        IObservable<IActorProxy> OnDisconnected { get; }

        Task<T> SendAsync<T>(Message request, IOperationContext context) where T : Message;
        void SendOneWay(Message msg, IOperationContext context);
    }
}