using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using Photon.SocketServer;
using MOUSE.Core;
using Autofac;
using Autofac.Integration.Mef;
using System.Reflection;
using Photon.SocketServer.ServerToServer;
using PhotonHostRuntimeInterfaces;

namespace PhotonAdapter
{
    public class NodePhotonApplication : ApplicationBase, INetProvider
    {
        private INetNode _node;

        ConcurrentDictionary<uint, ClientPeer> _clientPeers = new ConcurrentDictionary<uint, ClientPeer>();

        protected override void Setup()
        {
            var builder = new ContainerBuilder();

            builder.RegisterComposablePartCatalog(new DirectoryCatalog(""));
            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>();
            builder.RegisterType<ServiceRepository>().As<IServiceRepository>();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>();
            builder.RegisterType<EntityClusterNode>().As<INetNode>();
            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>();
            builder.RegisterInstance(this).As<INetProvider>;

            var container = builder.Build();
            _node = container.Resolve<INetNode>();

            _node.Start(false);


        }

        protected override void TearDown()
        {
            _node.Stop();
        }

        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
                var photonPeer = new ClientPeer(this, initRequest.Protocol, initRequest.PhotonPeer, (uint)initRequest.ConnectionId);
                _clientPeers.Add(photonPeer.NetId, photonPeer);
                ((INetPeerFactory)_node).OnNetConnect(photonPeer.NetId);
                return photonPeer;
        }

        public void OnClientPeerDisconnected(ClientPeer photonPeer)
        {
            _clientPeers.Remove(photonPeer.NetId);
            ((INetPeerFactory)_node).OnNetDisconnect(photonPeer.NetId);
        }

        public void OnClientPeerOperationRequest(ClientPeer clientPeer, OperationRequest operationRequest, SendParameters sendParameters)
        {
            NativeReader reader = new NativeReader();
            reader.SetBuffer((byte[])operationRequest.Parameters[0], 0);
            ((INetPeerFactory)_node).OnNetData(clientPeer.NetId, reader);
        }

        #region INetPeer
        public bool Startup(System.Net.IPEndPoint listenEndpoint, int maxConnections)
        { }

        public void Shutdown()
        { }

        public void Connect(System.Net.IPEndPoint target)
        {
            _photonApp.ConnectToServer(target, "MOUSE", null);
        }

        public void CloseConnection(int netIndex)
        {
            _clientPeers[netIndex].Disconnect();
        }

        public bool ProcessNetEvent(INetPeerFactory processor)
        {
            return false;
        }

        public void Send(int netId, byte[] buff, int length, MessagePriority messagePriority, MessageReliability messageReliability)
        {
            throw new NotImplementedException();
        }

        public System.Net.IPEndPoint GetEndPointOf(int netId)
        {
            throw new NotImplementedException();
        }

        public System.Net.IPEndPoint EndPoint
        {
            get { return null; }
        }
        #endregion

        
    }

    public class ClientPeer : PeerBase
    {
        public readonly uint NetId;
        public readonly NodePhotonApplication _app;
        

        public ClientPeer(NodePhotonApplication app, IRpcProtocol protocol, IPhotonPeer unmanagedPeer, uint netId)
            : base(protocol, unmanagedPeer)
        {
            NetId = netId;
            _app = app;
        }

        protected override void OnDisconnect()
        {
            _app.OnClientPeerDisconnected(this);
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            _app.OnClientPeerOperationRequest(this, operationRequest, sendParameters);
        }
    }
}
