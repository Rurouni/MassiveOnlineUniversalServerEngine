using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using MOUSE.Core.Interfaces;

namespace MOUSE.Core.Azure.ServiceFabric
{
    public class ServiceFabricCommunicationListenerAdapter : ICommunicationListener
    {
        readonly INetNode _netNode;
        readonly string _transportSchema;

        public ServiceFabricCommunicationListenerAdapter(INetNode netNode, string transportSchema = "net.tcp")
        {
            _netNode = netNode;
            _transportSchema = transportSchema;
        }

        public void Abort()
        {
            _netNode.Dispose();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            _netNode.Dispose();
            return Task.FromResult(0);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            _netNode.Start();
            var ipAddress = FabricRuntime.GetNodeContext().IPAddressOrFQDN;
            return Task.FromResult($"{_transportSchema}://{ipAddress}:{_netNode.Address.Port}");
        }
    }
}