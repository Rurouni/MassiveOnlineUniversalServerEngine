using System;
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
    public class NodePhotonApplication : ApplicationBase
    {
        private INode _node;
        private PhotonPeerInterface _netPeer;
        private object syncThis = new object();

        protected override void Setup()
        {
            lock (syncThis)
            {
                var builder = new ContainerBuilder();

                builder.RegisterComposablePartCatalog(new DirectoryCatalog(""));
                builder.RegisterType<EntityDomain>().As<IEntityDomain>();
                builder.RegisterType<EntityRepository>().As<IEntityRepository>();
                builder.RegisterType<MessageFactory>().As<IMessageFactory>();
                builder.RegisterType<EntityClusterNode>().As<INode>();
                builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>();
                _netPeer = new PhotonPeerInterface(this);
                builder.RegisterInstance(_netPeer).As<INetPeer>;

                var container = builder.Build();
                _node = container.Resolve<INode>();

                _node.Connect
            }

            
        }

        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
            lock (syncThis)
            {
                return new SharedPeer(initRequest.Protocol, initRequest.PhotonPeer, initRequest.ConnectionId);
            }
        }

        protected override ServerPeerBase CreateServerPeer(InitResponse initResponse, object state)
        {
            lock (syncThis)
            {
                return new SharedPeer(initRequest.Protocol, initRequest.PhotonPeer, initRequest.ConnectionId);
            }
        }

        protected override void TearDown()
        {
            lock (syncThis)
            {

            }
        }
    }

    public class SharedPeer : PeerBase
    {
        public readonly uint NetId;

        public SharedPeer(IRpcProtocol protocol, IPhotonPeer unmanagedPeer, uint netId) : base(protocol, unmanagedPeer)
        {
            NetId = netId;
        }

        protected override void OnDisconnect()
        {
            throw new NotImplementedException();
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            this.
            throw new NotImplementedException();

        }
    }

    public class SomePeer: ServerPeerBase
    {

        protected override void OnEvent(IEventData eventData, SendParameters sendParameters)
        {
            throw new NotImplementedException();
        }

        protected override void OnOperationResponse(OperationResponse operationResponse, SendParameters sendParameters)
        {
            throw new NotImplementedException();
        }

        protected override void OnDisconnect()
        {
            throw new NotImplementedException();
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            throw new NotImplementedException();
        }
    }
}
