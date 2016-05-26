using System.Net;
using EventSourceProxy;
using Lidgren.Network;

namespace LidgrenWrap
{
    [EventSourceImplementation(Name = "LidgrenEvents")]
    public interface ILidgrenEvents
    {
        void ConnectingTo(IPEndPoint target);
        void Warning(string msg);
        void Error(string msg);
        void Debug(string msg);
        void StatusChanged(string status, long connectionId);
        void OnDataReceived(long connectionId, int numBytes);
    }

    static public class LidgrenEventsETWLogger
    {
        static LidgrenEventsETWLogger()
        {
            EventSourceImplementer.RegisterProvider<ILidgrenEvents>(new ToStringObjectSerializer());

            TraceParameterProvider.Default
                .For<ILidgrenEvents>()
                    .With<IPEndPoint>().Trace(x => x.ToString());

            Instance = EventSourceImplementer.GetEventSourceAs<ILidgrenEvents>();
        }

        static public ILidgrenEvents Instance { get; }
    }

}