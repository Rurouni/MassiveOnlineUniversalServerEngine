using System;
using System.Net;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MOUSE.Core.Logging.Serilog
{
    public class SerilogCoreEvents : ICoreEvents
    {
        readonly ILogger _logger;

        public SerilogCoreEvents(ILogger logger)
        {
            _logger = logger.ForContext<ICoreEvents>();
        }

        public void NodeStarted(INetNode node)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("NetNode {@node} has started", node);
        }

        public void NodeUpdateLoopStarted(INetNode node)
        {
            if (_logger.IsEnabled(LogEventLevel.Information))
                _logger.Information("Update loop of NetNode {@node} has started", node);
        }

        public void NodeStopped(INetNode node)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("NetNode {@node} has stopped", node);
        }

        public void NodeUpdateLoopError(INetNode node, Exception exception)
        {
            if (_logger.IsEnabled(LogEventLevel.Error))
                _logger.Error("NetNode {@node} unexpected exception {@exception}", node, exception);
        }

        public void NetChannelConnected(INetNode node, INetChannel channel)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetNode {@node} NetChannel {@channel} has connected", node,  channel);
        }

        public void NetChannelDisconnected(INetNode node, INetChannel channel)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetNode {@node} NetChannel {@channel} has disconnected", node, channel);
        }

        public void ConnectingTo(INetNode node, IPEndPoint address)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetNode {@node} is trying to connect to {@address}", node, address);
        }

        public void ConnectionHasTimedOut(INetNode node, IPEndPoint address)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("NetNode {@node} has failed to connect to {@address} within alloted time", node, address);
        }

        public void ConnectionSucceeded(INetNode node, IPEndPoint address, INetChannel channel, int durationMs)
        {
            if (_logger.IsEnabled(LogEventLevel.Information))
                _logger.Information("NetNode {@node} has connected to {@address} with {@channel} in {durationMs} ms", node, address, channel, durationMs);
        }

        public void NetChannelRequestStarted(INetNode node, INetChannel channel, Message request, Guid requestId)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetNode {@node} on NetChannel {@channel} sending request {@message} with id {requestId}", node, channel, request, requestId);
        }

        public void NetChannelSentReplyMessage(INetNode node, INetChannel channel, Message reply, IOperationContext operation)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetNode {@node} on NetChannel {@channel} sent reply {@message} for operation {@operation}", node, channel, reply, operation);
        }

        public void CantReplyToOneWayMessage(INetNode node, IOperationContext operation)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Warning("NetChannel {@channel} is trying to reply to one way message {@message}", operation.ReplyChannel, operation.Message);
        }

        public void OperationProcessingFailed(INetNode node, INetChannel channel, IOperationContext operation, Exception exception)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("NetNode {@node} on NetChannel {@channel} failed to process operation {@operation} with {@exception}", node, channel, operation, exception);
        }

        public void NetChannelRequestFailed(INetNode node, NetChannel channel, ErrorMessage error, Guid requestId, int durationMs)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("NetNode {@node} NetChannel {@channel} sent request failed with {@error} for {requestId} in {durationMs} ms", node, channel, error, requestId, durationMs);
        }


        public void NetChannelRequestCompleted(INetNode node, INetChannel channel, Message message, Guid requestId, int durationMs)
        {
            if (_logger.IsEnabled(LogEventLevel.Information))
                _logger.Information("NetNode {@node} NetChannel {@channel} is received reply {@message} for {requestId} in {durationMs} ms", node, channel, message, requestId, durationMs);
        }

        public void NetChannelIsDisposing(INetNode node, INetChannel channel)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetChannel {@channel} is closing", channel);
        }

        public void NetChannelHasReceivedMessage(INetNode node, INetChannel channel, Message message)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetChannel {@channel} has received message {@message}", channel, message);
        }

        public void OperationProcessed(INetNode node, INetChannel channel, IOperationContext operation)
        {
            if (_logger.IsEnabled(LogEventLevel.Information))
                _logger.Information("NetNode {@node} on NetChannel {@channel} processed {@operation}", node, channel, operation);
        }

        public void NetChannelRequestTimedOut(INetNode node, INetChannel channel, Message message, Guid requestId, int durationMs)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("NetChannel {@channel} has timed out within {durationMs} ms on sending request {@message} {requestId}", channel, durationMs, message, requestId);
        }

        public void NetChannelSentMessage(INetNode node, INetChannel channel, Message message)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Debug("NetChannel {@channel} has sent message {@message}", channel, message);
        }
    }
}
