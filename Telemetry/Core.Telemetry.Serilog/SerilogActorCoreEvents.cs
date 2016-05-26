using System;
using MOUSE.Core.Actors;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Logging;
using Serilog;
using Serilog.Events;

namespace MOUSE.Core.Logging.Serilog
{
    public class SerilogActorCoreEvents : IActorCoreEvents
    {
        readonly ILogger _logger;

        public SerilogActorCoreEvents(ILogger logger)
        {
            _logger = logger.ForContext<IActorCoreEvents>();
        }

        public void ReceivedMessageForActor(IActor actor, IOperationContext operation)
        {
            if(_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("Received message:{@message} for actor:{@actor} from NetChannel:{@channel}", operation.Message, actor, operation.ReplyChannel);
        }

        public void ReceivedMessageForUnknownActor(ActorKey actorKey, IOperationContext operation)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("Received message:{@message} for unknown actor:{@actorKey} from NetChannel:{@channel}", operation.Message, actorKey, operation.ReplyChannel);
        }

        public void ReceivedNonActorMessage(IOperationContext operation)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("Received message:{@message} without actor header from NetChannel {@channel}", operation.Message, operation.ReplyChannel);
        }

        public void ActorOperationFailed(IActor actor, Exception exception, IOperationContext operation, double durationMs)
        {
            _logger.Warning("actor:{@actor} has failed to process message:{@message} in {durationMs} ms with exception:{exception}", actor, operation.Message, durationMs, exception);
        }

        public void ActorOperationSucceeded(IActor actor, IOperationContext operation, double durationMs)
        {
            if (_logger.IsEnabled(LogEventLevel.Information))
                _logger.Information("actor:{@actor} has successfully processed message:{@message} in {durationMs} ms", actor, operation.Message, durationMs);
        }

        public void ActorActivated(INetNode node, ActorKey actorKey)
        {
            if (_logger.IsEnabled(LogEventLevel.Information))
                _logger.Information("actor:{@actorKey} is activated on node:{@node}", actorKey, node);
        }
    }
}