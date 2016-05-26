using System;
using MOUSE.Core.Actors;
using MOUSE.Core.Azure;
using Serilog;

namespace MOUSE.Core.Logging.Serilog
{
    public class SerilogAzureEventHubActorSystemEvents : IAzureEventHubActorSystemEvents
    {
        readonly ILogger _logger;

        public SerilogAzureEventHubActorSystemEvents(ILogger logger)
        {
            _logger = logger.ForContext<IAzureEventHubActorSystemEvents>();
        }

        public void ReceivedUnboundReply(ushort actorSystemId, Message message)
        {
            _logger.Warning("EventHubActorSystem:{actorSystemId} received non awaited reply:{@message}", actorSystemId, message);
        }

        public void FailedToDeserializeEvent(ushort actorSystemId, string eventPartitionKey, long eventSequenceNumber, string eventOffset)
        {
            _logger.Warning("EventHubActorSystem:{actorSystemId} failed to deserialise event:{{ partition:{partitionId}, sequence:{sequenceNumber}, offset:{offset} }}",
                actorSystemId, eventPartitionKey, eventSequenceNumber, eventOffset);
        }

        public void ReceivedMessageWithoutActorHeader(ushort actorSystemId, Message message)
        {
            _logger.Warning("EventHubActorSystem:{actorSystemId} received message:{@message} without actor header", actorSystemId, message);
        }

        public void EventProcessorStopped(string reason, string eventHubPath, string consumerGroupName, string partitionId, string offset, long epoch)
        {
            _logger.Information("Event processor path:{eventHubPath} consumerGroup:{consumerGroupName} partition:{partitionId} offset:{offset} epoch:{epoch} has stopped with {reason}",
                eventHubPath, consumerGroupName, partitionId, offset, epoch, reason);
        }

        public void EventProcessorStarted(string eventHubPath, string consumerGroupName, string partitionId, string offset, long epoch)
        {
            _logger.Information("Event processor path:{eventHubPath} consumerGroup:{consumerGroupName} partition:{partitionId} offset:{offset} epoch:{epoch} has started",
                eventHubPath, consumerGroupName, partitionId, offset, epoch);
        }

        public void ActorSystemStarted(ushort actorSystemId)
        {
            _logger.Warning("EventHubActorSystem:{actorSystemId} has started", actorSystemId);
        }

        public void EventProcessorHostException(string action, Exception exception)
        {
            _logger.Warning("EventProcessorHost on action:{action} has encountered internal exception:{exception}", action, exception);
        }

        public void EventProcessorCheckpointed(string eventHubPath, string consumerGroupName, string partitionId, string offset, long epoch)
        {
            _logger.Information("Event processor path:{eventHubPath} consumerGroup:{consumerGroupName} partition:{partitionId} offset:{offset} epoch:{epoch} has created a checkpoint",
                eventHubPath, consumerGroupName, partitionId, offset, epoch);
        }

        public void FailedToSendReply(ActorKey actorKey, Message message, Exception exception)
        {
            _logger.Warning("Actor:{@actorKey} whilst processing message:{@message} has failed to send reply because of exception:{exception}", actorKey, message, exception);
        }

        public void ReceivedMessageForActor(ActorKey actorKey, Message message)
        {
            _logger.Warning("Actor:{@actorKey} is processing message:{@message}", actorKey, message);
        }

        public void ReceivedExpiredMessage(DateTime timestamp)
        {
            _logger.Warning("Recieved expired message with timestamp:{@timestamp}", timestamp);
        }

        public void FailedToSendMessage(Guid requestId, Message message, Exception exception)
        {
            _logger.Warning("Failed to send message:{requestId} {@message} with {exception} ", requestId, message, exception);
        }
    }
}