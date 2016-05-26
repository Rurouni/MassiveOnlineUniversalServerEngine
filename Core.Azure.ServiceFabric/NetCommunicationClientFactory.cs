using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.FabricTransport.Client;
using MOUSE.Core.Interfaces;

namespace MOUSE.Core.Azure.ServiceFabric
{
    public class NetCommunicationClientFactory : CommunicationClientFactoryBase<NetCommunicationClient>
    {
        private readonly INetNode _netNode;

        public NetCommunicationClientFactory(INetNode netNode, IServicePartitionResolver servicePartitionResolver = null, IEnumerable<IExceptionHandler> exceptionHandlers = null, string traceId = null)
            : base(servicePartitionResolver, AddExceptionHandler(exceptionHandlers), traceId)
        {
            _netNode = netNode;

        }

        static IEnumerable<IExceptionHandler> AddExceptionHandler(IEnumerable<IExceptionHandler> handlers)
        {
            var newHandlers = new List<IExceptionHandler>() {new NetExceptionHandler()};
            if (handlers!= null)
                newHandlers.AddRange(handlers);
            return newHandlers;
        }   

        protected override void AbortClient(NetCommunicationClient client)
        {
            client.Channel.Dispose();
        }

        async protected override Task<NetCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            var channel = await _netNode.Connect(endpoint, cancellationToken);

            return new NetCommunicationClient(channel);
        }

        protected override bool ValidateClient(NetCommunicationClient clientChannel)
        {
            return clientChannel.Channel.IsConnected;
        }

        protected override bool ValidateClient(string endpoint, NetCommunicationClient client)
        {
            return client.Channel.IsConnected && client.Channel.TransportChannel.EndPoint.ToString() == endpoint;
        }
    }

    public class NetExceptionHandler : IExceptionHandler
    {
        bool IExceptionHandler.TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            if (exceptionInformation.Exception is TimeoutException ||
                exceptionInformation.Exception is ConnectionFailedException ||
                exceptionInformation.Exception is OperationCanceledException )
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, 1);
                return true;
            }

            else if (exceptionInformation.Exception is ProcessingException)
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, 0);
                return true;
            }
            result = null;
            return false;
        }
    }
}