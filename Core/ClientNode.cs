using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MOUSE.Core.Fibers;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Fibers;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.MessageProcessing;
using MOUSE.Core.Serialization;

namespace MOUSE.Core
{
    public class ClientNode : NetNode<NetChannel>
    {
        readonly Action<ISimpleMessageHandlingConfigBuilder> _messageHandlingConfigurator;
        readonly IBufferPool _bufferPool;
        readonly IFiber _fiber;
        readonly List<IPEndPoint> _serverEndpoints = new List<IPEndPoint>();
        

        INetChannel _mainConnection;
        public ClientNode(IEnumerable<string> servers, INetProvider net, ICoreEvents logger, IMessageSerializer messageSerializer,
            Action<ISimpleMessageHandlingConfigBuilder> messageHandlingConfigurator,
            IFiber fiber = null, INetNodeConfig config = null, IBufferPool bufferPool = null)
            : base("client", net, logger, messageSerializer, null, config)
        {
            _bufferPool = bufferPool ?? new WcfBufferPool();
            _messageHandlingConfigurator = messageHandlingConfigurator;

            if (fiber == null)
            {
                _fiber = 
                    SynchronizationContext.Current == null 
                    ? new SimpleFiber() 
                    : new SimpleFiber(TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                _fiber = fiber;


            _serverEndpoints.AddRange(servers.Select(EndpointHelpers.Parse));
            ChannelFactory = CreatePeer;
        }

        NetChannel CreatePeer(INetNode node, ITransportChannel transport)
        {
            var peer = new AsyncProcessingNetChannel( builder => builder
                .UseFiber(_fiber)
                .UseConfigurableClientDispatcher((handlersBuilder) =>
                {
                    _messageHandlingConfigurator(handlersBuilder);
                    return handlersBuilder;
                }), node, transport, MessageSerializer, Logger, Config, _bufferPool);
            return peer;
        }

        async Task EnsureConnected()
        {
            if (_mainConnection == null)
            {
                //TODO: implement proper connection restore/cycle
                _mainConnection = await Connect(_serverEndpoints[0]);
                _mainConnection.DisconnectedEvent.Subscribe(_ => _mainConnection = null);
            }
        }

        async public Task<Message> SendRequestAsync(Message request)
        {
            await EnsureConnected();
            var reply = await _mainConnection.SendRequestAsync(request);
            await _fiber.ContinueOn();
            return reply;
        }

        async public Task<TReply> SendRequestAsync<TReply>(Message request)
            where TReply : Message
        {
            await EnsureConnected();
            var reply = await _mainConnection.SendRequestAsync<TReply>(request);
            await _fiber.ContinueOn();
            return reply;
        }

        //beware: truly fire and forget async void flow, you'll never know if it failed
        async public void SendOneWay(Message msg)
        {
            try
            {
                await EnsureConnected();
                _mainConnection.Send(msg);
            }
            catch (Exception)
            {
                Logger.ConnectionHasTimedOut(this, _serverEndpoints[0]);
            }
        }
    }
}