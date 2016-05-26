using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MOUSE.Core.Azure
{
    class DelegatingEventProcessor : IEventProcessor
    {
        readonly Func<EventData, Task> _processor;
        readonly IAzureEventHubActorSystemEvents _logger;
        readonly TimeSpan _checkpointTimeout;

        DateTime _lastCheckpointTime;

        public DelegatingEventProcessor(TimeSpan checkpointTimeout, Func<EventData, Task> processor, IAzureEventHubActorSystemEvents logger)
        {
            _processor = processor;
            _logger = logger;
            _checkpointTimeout = checkpointTimeout;
        }

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            _logger.EventProcessorStopped(reason.ToString(), context.EventHubPath, context.ConsumerGroupName, context.Lease.PartitionId, context.Lease.Offset, context.Lease.Epoch);

            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
                _logger.EventProcessorCheckpointed(context.EventHubPath, context.ConsumerGroupName, context.Lease.PartitionId, context.Lease.Offset, context.Lease.Epoch);
            }
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            _lastCheckpointTime = DateTime.UtcNow;
            _logger.EventProcessorStarted(context.EventHubPath, context.ConsumerGroupName, context.Lease.PartitionId, context.Lease.Offset, context.Lease.Epoch);
            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            await Task.WhenAll(messages.Select(eventData => _processor(eventData)));

            //if (_lastCheckpointTime.Add(_checkpointTimeout) < DateTime.UtcNow)
            //{
                await context.CheckpointAsync();
                _logger.EventProcessorCheckpointed(context.EventHubPath, context.ConsumerGroupName, context.Lease.PartitionId, context.Lease.Offset, context.Lease.Epoch);
            //    _lastCheckpointTime = DateTime.UtcNow;
            //}
        }
    }
}