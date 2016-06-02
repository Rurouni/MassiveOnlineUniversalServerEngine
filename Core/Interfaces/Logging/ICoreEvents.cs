using System;
using System.Net;

namespace MOUSE.Core.Interfaces.Logging
{
    public interface ICoreEvents
    {
        void NodeStarted(INetNode node);
        void NodeUpdateLoopStarted(INetNode node);
        void NodeStopped(INetNode node);
        void NodeUpdateLoopError(INetNode node, Exception exception);
        void NetChannelConnected(INetNode node, INetChannel channel);
        void NetChannelDisconnected(INetNode node, INetChannel channel);
        void NetChannelIsDisposing(INetNode node, INetChannel channel);
        void ConnectingTo(INetNode node, IPEndPoint address);
        void ConnectionHasTimedOut(INetNode node, IPEndPoint address);
        void ConnectionSucceeded(INetNode node, IPEndPoint address, INetChannel channel, int durationMs);
        void NetChannelSentMessage(INetNode node, INetChannel channel, Message message);
        void NetChannelRequestStarted(INetNode node, INetChannel channel, Message request, Guid requestId);
        void NetChannelSentReplyMessage(INetNode node, INetChannel channel, Message reply, IOperationContext context);
        void CantReplyToOneWayMessage(INetNode node, IOperationContext operation);
        void NetChannelRequestTimedOut(INetNode node, INetChannel channel, Message request, Guid requestId, int durationMs);
        void NetChannelRequestCompleted(INetNode node, INetChannel channel, Message reply, Guid requestId, int durationMs);
        void NetChannelHasReceivedMessage(INetNode node, INetChannel channel, Message message);
        void OperationProcessed(INetNode node, INetChannel channel, IOperationContext operation);
        void OperationProcessingFailed(INetNode node, INetChannel channel, IOperationContext operation, Exception exception);
        void NetChannelRequestFailed(INetNode node, NetChannel netChannel, ErrorMessage error, Guid requestId, int durationMs);
        
    }
}