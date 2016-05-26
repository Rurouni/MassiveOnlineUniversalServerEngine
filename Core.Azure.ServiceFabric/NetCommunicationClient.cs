using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.Client;
using MOUSE.Core.Interfaces;

namespace MOUSE.Core.Azure.ServiceFabric
{
    public class NetCommunicationClient : ICommunicationClient
    {
        public INetChannel Channel { get; }

        public ResolvedServiceEndpoint Endpoint { get; set; }

        public string ListenerName { get; set; }

        public ResolvedServicePartition ResolvedServicePartition { get; set; }

        public NetCommunicationClient(INetChannel channel)
        {
            Channel = channel;
        }
    }
}