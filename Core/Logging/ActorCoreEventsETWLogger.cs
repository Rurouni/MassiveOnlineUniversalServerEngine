using EventSourceProxy.NuGet;
using MOUSE.Core.Interfaces.Logging;

namespace MOUSE.Core.Logging
{
    static public class ActorCoreEventsETWLogger
    {
        static ActorCoreEventsETWLogger()
        {
            EventSourceImplementer.RegisterProvider<IActorCoreEvents>(new ToStringObjectSerializer());

            //TraceParameterProvider.Default
            //    .For<IActorCoreEvents>()
            //        .With<IActorProxy>()
            //            .Trace(x => x.ActorRef)
            //        .With<IOperationContext>()
            //            .Trace(x => x.ActivityId)
            //            .Trace(x => x.Message)
            //            .Trace(x => x.ReplyChannel)
            //        .With<INetChannel>()
            //            .Trace(x => x.TransportChannel.LocalId)
            //            .Trace(x => x.TransportChannel.EndPoint);

            Instance = EventSourceImplementer.GetEventSourceAs<IActorCoreEvents>();
        }

        static public IActorCoreEvents Instance { get; }
    }
}