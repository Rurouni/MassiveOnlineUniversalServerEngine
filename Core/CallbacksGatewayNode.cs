using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;

namespace MOUSE.Core
{
    public class CallbacksGatewayNode : NetNode<NetChannel>, ICallbacksGatewayNode
    {
        readonly ConcurrentDictionary<Guid, INetChannel> _callbackChannels = new ConcurrentDictionary<Guid, INetChannel>();
        
        public CallbacksGatewayNode(string name, INetProvider net, ICoreEvents coreLogger, IMessageSerializer messageSerializer,
            INetNodeConfig config, IBufferPool bufferPool, IPEndPoint publicAddress = null)
            : base(name, net, coreLogger, messageSerializer, null, config, publicAddress)
        {
            ChannelFactory = (node, transport) =>
            {
                var channel = new NetChannel(node, transport, messageSerializer, coreLogger, config, bufferPool);
                channel.OperationReceivedEvent.Subscribe(ProcessCallbackMessage);
                return channel;
            };
        }


        public CallbackChannelRef ExposeAsCallbackChannel(INetChannel channel)
        {
            var callbackId = Guid.NewGuid();
            _callbackChannels.TryAdd(callbackId, channel);//should always succeed as key is GUID
            channel.DisconnectedEvent.Subscribe(_ => OnSourceChannelDisconnect(callbackId));
            return new CallbackChannelRef(Address, callbackId);
        }

        void OnSourceChannelDisconnect(Guid callbackId)
        {
            INetChannel dummy;
            _callbackChannels.TryRemove(callbackId, out dummy);
            var notification = new CallbackChannelDisconnected(callbackId);

            //TODO: optimize later to only send about ones who somebody is interested in
            foreach (INetChannel netChannel in OpenedChannels)
            {
                netChannel.Send(notification);
            }
        }

        void ProcessCallbackMessage(IOperationContext operation)
        {
            var callbackHeader = operation.Message.GetHeader<CallbackHeader>();
            if (callbackHeader != null)
            {
                INetChannel callbackChannel;
                if (_callbackChannels.TryGetValue(callbackHeader.CallbackChannelId, out callbackChannel))
                {
                    operation.Message.RemoveHeader<CallbackHeader>();
                    callbackChannel.Send(operation.Message);
                }
                else
                {
                    operation.ReplyChannel.Send(new CallbackChannelDisconnected(callbackHeader.CallbackChannelId));
                }
            }
        }

        async public Task<INetChannel> ConnectToCallbackChannel(CallbackChannelRef callbackChannelRef)
        {
            if (callbackChannelRef.Address.Equals(Address))
            {
                INetChannel callbackChannel;
                if (_callbackChannels.TryGetValue(callbackChannelRef.CallbackChannelId, out callbackChannel))
                {
                    return callbackChannel;
                }
                else
                {
                    throw new ProcessingException(BasicErrorCode.ConnectionFailed);
                } 
            }
            else
            {
                var coreChannel = await Connect(callbackChannelRef.Address);

                return new CallbackProxyChannel(coreChannel, callbackChannelRef);
            }
        }
    }
}