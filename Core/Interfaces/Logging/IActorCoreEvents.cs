using System;
using EventSourceProxy.NuGet;
using MOUSE.Core.Actors;
using MOUSE.Core.Interfaces.Actors;

namespace MOUSE.Core.Interfaces.Logging
{
    [EventSourceImplementation(Name = "MOUSE-ActorCoreEvents")]
    public interface IActorCoreEvents
    {
        void ReceivedMessageForActor(IActor actor, IOperationContext operation);
        void ReceivedMessageForUnknownActor(ActorKey actorKey, IOperationContext operation);
        void ReceivedNonActorMessage(IOperationContext operation);

        void ActorOperationFailed(IActor actor, Exception exception, IOperationContext operation, double durationMs);
        void ActorOperationSucceeded(IActor actor, IOperationContext operation, double durationMs);
        void ActorActivated(INetNode node, ActorKey actorKey);
    }
}