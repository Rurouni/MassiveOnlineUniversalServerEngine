using System;
using EventSourceProxy.NuGet;
using MOUSE.Core.Actors;

namespace MOUSE.Core.Azure
{
    [EventSourceImplementation(Name = "MOUSE-EventHubActorSystemEvents")]
    public interface IAzureEventHubActorSystemEvents
    {
        void ReceivedUnboundReply(ushort actorSystemId, Message message);
        void FailedToDeserializeEvent(ushort actorSystemId, string eventPartitionKey, long eventSequenceNumber, string eventOffset);
        void ReceivedMessageWithoutActorHeader(ushort actorSystemId, Message message);
        void EventProcessorStopped(string reason, string eventHubPath, string consumerGroupName, string partitionId, string offset, long epoch);
        void EventProcessorStarted(string eventHubPath, string consumerGroupName, string partitionId, string offset, long epoch);
        void ActorSystemStarted(ushort actorSystemId);
        void EventProcessorHostException(string action, Exception exception);
        void EventProcessorCheckpointed(string eventHubPath, string consumerGroupName, string partitionId, string offset, long epoch);
        void FailedToSendReply(ActorKey actorKey, Message message, Exception exception);
        void ReceivedMessageForActor(ActorKey actorKey, Message message);
        void ReceivedExpiredMessage(DateTime timestamp);
        void FailedToSendMessage(Guid requestId, Message msg, Exception exception);
    }

    static public class EventHubActorSystemEventsETWLogger
    {
        static EventHubActorSystemEventsETWLogger()
        {
            EventSourceImplementer.RegisterProvider<IAzureEventHubActorSystemEvents>(new ToStringObjectSerializer());

            Instance = EventSourceImplementer.GetEventSourceAs<IAzureEventHubActorSystemEvents>();
        }

        static public IAzureEventHubActorSystemEvents Instance { get; }
    }
}