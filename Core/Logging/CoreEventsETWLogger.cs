using EventSourceProxy.NuGet;
using MOUSE.Core.Interfaces.Logging;

namespace MOUSE.Core.Logging
{
    static public class CoreEventsETWLogger
    {
        static CoreEventsETWLogger()
        {
            EventSourceImplementer.RegisterProvider<ICoreEvents>(new ToStringObjectSerializer());

            //TraceParameterProvider.Default
            //    .For<ICoreEvents>()
            //        .With<IPEndPoint>().Trace(x => x.ToString())
            //        .With<INetNode>()
            //            .Trace(x => x.InstanceName)
            //            .Trace(x => x.Address)
            //        .With<IOperationContext>()
            //            .Trace(x => x.ActivityId)
            //            .Trace(x => x.Message)
            //            .Trace(x => x.ReplyChannel.TransportChannel.LocalId).As("replyChannelId")
            //        .With<INetChannel>()
            //            .Trace(x => x.TransportChannel.LocalId)
            //            .Trace(x => x.TransportChannel.EndPoint);

            Instance = EventSourceImplementer.GetEventSourceAs<ICoreEvents>();
        }

        static public ICoreEvents Instance { get; }
    }
}