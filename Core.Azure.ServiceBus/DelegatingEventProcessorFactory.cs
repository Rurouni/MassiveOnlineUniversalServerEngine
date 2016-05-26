using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MOUSE.Core.Azure
{
    public class DelegatingEventProcessorFactory : IEventProcessorFactory
    {
        readonly TimeSpan _checkpointTimeout;
        readonly Func<EventData, Task> _processor;
        readonly IAzureEventHubActorSystemEvents _logger;

        public DelegatingEventProcessorFactory(TimeSpan checkpointTimeout, Func<EventData, Task> processor, IAzureEventHubActorSystemEvents logger)
        {
            _checkpointTimeout = checkpointTimeout;
            _processor = processor;
            _logger = logger;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new DelegatingEventProcessor(_checkpointTimeout, _processor, _logger);
        }
    }
}