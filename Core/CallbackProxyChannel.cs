using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;

namespace MOUSE.Core
{
    public class CallbackProxyChannel : INetChannel
    {
        readonly INetChannel _remoteNodeChannel;
        readonly CallbackChannelRef _callbackRef;

        public CallbackProxyChannel(INetChannel remoteNodeChannel, CallbackChannelRef callbackRef)
        {
            _remoteNodeChannel = remoteNodeChannel;
            _callbackRef = callbackRef;
        }

        public void Dispose()
        {
            throw new NotSupportedException();
        }

        public void Init(ITransportChannel channel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
        {
            throw new NotSupportedException();
        }

        public ITransportChannel TransportChannel
        {
            get { throw new NotSupportedException(); }
        }

        public IObservable<INetChannel> DisconnectedEvent
        {
            get
            {
                return _remoteNodeChannel.DisconnectedEvent
                    .Amb(_remoteNodeChannel.OperationReceivedEvent
                        .Where(op => op.Message is CallbackChannelDisconnected && ((CallbackChannelDisconnected)op.Message).CallbackChannelId == _callbackRef.CallbackChannelId)
                        .Select(op => op.ReplyChannel)
                        .FirstAsync());
            }
        }

        public IObservable<OperationContext> OperationReceivedEvent
        {
            get { throw new NotSupportedException();}
        }

        public bool IsConnected => _remoteNodeChannel.IsConnected;

        public Task<Message> SendRequestAsync(Message request)
        {
            throw new NotSupportedException();
        }

        public Task<TReply> SendRequestAsync<TReply>(Message request) where TReply : Message
        {
            throw new NotSupportedException();
        }

        public void Send(Message msg)
        {
            msg.AttachHeader(new CallbackHeader(_callbackRef.CallbackChannelId));
            _remoteNodeChannel.Send(msg);
        }

        public void ReplyWithError(IOperationContext context, ushort errorCode, string errorDesc)
        {
            throw new NotSupportedException();
        }

        public void Reply(IOperationContext context, Message replyMsg)
        {
            throw new NotSupportedException();
        }

        public IDisposable RegisterHandler<TMessage>(Action<TMessage> messageHandler) where TMessage : Message
        {
            throw new NotSupportedException();
        }
    }
}