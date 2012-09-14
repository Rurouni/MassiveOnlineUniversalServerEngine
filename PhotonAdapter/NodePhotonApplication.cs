using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Text;
using Photon.SocketServer;
using MOUSE.Core;
using Autofac;
using Autofac.Integration.Mef;
using System.Reflection;
using Photon.SocketServer.ServerToServer;
using PhotonHostRuntimeInterfaces;
using System.IO;

namespace PhotonAdapter
{
    public abstract class PhotonServerHostApplication : ApplicationBase, INetProvider
    {
        private IServerNode _node;

        protected abstract IServerNode CreateNode();

        protected override void Setup()
        {
            _node = CreateNode();
            _node.Start();
        }

        protected override void TearDown()
        {
            if(_node != null)
                _node.Stop();
        }

        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
            var photonPeer = new PhotonClientPeer(initRequest);
            photonPeer.ChannelListener = ((INetChannelConsumer)_node.ExternalNet).OnNetConnect(photonPeer);
            var localEndpoint = new IPEndPoint(IPAddress.Parse(initRequest.LocalIP), initRequest.LocalPort);
            Console.WriteLine("New client peer on "+localEndpoint);
            return photonPeer;
        }

        protected override ServerPeerBase CreateServerPeer(InitResponse initResponse, object state)
        {
            var photonPeer = new PhotonServerPeer(initResponse);
            photonPeer.ChannelListener = ((INetChannelConsumer)_node.InternalNet).OnNetConnect(photonPeer);
            var localEndpoint = new IPEndPoint(IPAddress.Parse(initResponse.LocalIP), initResponse.LocalPort);
            Console.WriteLine("New server peer on " + localEndpoint);
            return photonPeer;
        }

        #region INetProvider
        public bool Init(INetChannelConsumer factory)
        {
            return true;
        }

        public void Shutdown()
        { }

        public void Connect(IPEndPoint target)
        {
            ConnectToServer(target, "MOUSE", null);
        }

        public bool PumpEvents()
        {
            return true;
        }

        public IPEndPoint EndPoint
        {
            get
            {
                return new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], 0);
            }
        }

        #endregion
    }

    public class PhotonClientPeer : PeerBase, INetChannel
    {
        public readonly uint NetId;
        public INetChannelListener ChannelListener;
        private IPEndPoint _endPoint;
        


        public PhotonClientPeer(InitRequest initRequest)
            : base(initRequest.Protocol, initRequest.PhotonPeer)
        {
            NetId = (uint)initRequest.ConnectionId;
            _endPoint = new IPEndPoint(IPAddress.Parse(initRequest.RemoteIP), initRequest.RemotePort);
            
        }

        protected override void OnDisconnect()
        {
            ChannelListener.OnDisconnected();
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            var reader = new BinaryReader(new MemoryStream((byte[])operationRequest.Parameters[0]));
            ChannelListener.OnNetData(reader);
        }

        public uint Id
        {
            get { return NetId; }
        }

        public IPEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public void Send(Message msg)
        {
            var data = new Dictionary<byte, object>();
            data[0] = msg.GetSerialized();
            
            bool isReliable = msg.Reliability == MessageReliability.Reliable || msg.Reliability == MessageReliability.ReliableOrdered;

            var response = new OperationResponse(42);
            response.Parameters = data;
            var sendParam = new SendParameters();
            sendParam.Unreliable = !isReliable;

            SendOperationResponse(response, sendParam);
        }

        public void Close()
        {
            Disconnect();
        }
    }

    public class PhotonServerPeer : ServerPeerBase, INetChannel
    {
        public readonly uint NetId;
        public INetChannelListener ChannelListener;
        private readonly IPEndPoint _endPoint;
        readonly OperationResponse _response = new OperationResponse(42);


        public PhotonServerPeer(InitResponse initResponse)
            : base(initResponse.Protocol, initResponse.PhotonPeer)
        {
            NetId = (uint)initResponse.ConnectionId;
            _endPoint = new IPEndPoint(IPAddress.Parse(initResponse.RemoteIP), initResponse.RemotePort);
        }

        protected override void OnDisconnect()
        {
            ChannelListener.OnDisconnected();
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            var reader = new BinaryReader(new MemoryStream((byte[])operationRequest.Parameters[0]));
            ChannelListener.OnNetData(reader);
        }

        public uint Id
        {
            get { return NetId; }
        }

        public IPEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public void Send(Message msg)
        {
            BinaryWriter writer = msg.GetSerialized();
            var arr = new byte[writer.Position];
            Array.Copy(writer.Buff, arr, writer.Position); //TODO:need workaround
            var data = new Dictionary<byte, object>();
            data[0] = arr;

            bool isReliable = msg.Reliability == MessageReliability.Reliable || msg.Reliability == MessageReliability.ReliableOrdered;
            _response.Parameters[0] = arr;
            var sendParam = new SendParameters();
            sendParam.Unreliable = !isReliable;

            SendOperationResponse(_response, sendParam);
        }

        public void Close()
        {
            Disconnect();
        }

        protected override void OnEvent(IEventData eventData, SendParameters sendParameters)
        {
            var reader = new BinaryReader(new MemoryStream((byte[])eventData.Parameters[0]));
            ChannelListener.OnNetData(reader);
        }

        protected override void OnOperationResponse(OperationResponse operationResponse, SendParameters sendParameters)
        {
            var reader = new BinaryReader(new MemoryStream((byte[])operationResponse.Parameters[0]));
            ChannelListener.OnNetData(reader);
        }
    }
}
