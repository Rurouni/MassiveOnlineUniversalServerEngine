using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;
using Photon.SocketServer;

namespace PhotonAdapter
{
    public class PhotonPeerInterface : INetPeer
    {
        private ApplicationBase _photonApp;

        public PhotonPeerInterface(ApplicationBase photonApp)
        {
            _photonApp = photonApp;
        }

        public bool Startup(System.Net.IPEndPoint listenEndpoint, int maxConnections)
        {}

        public void Shutdown()
        {}

        public void Connect(System.Net.IPEndPoint target)
        {
            _photonApp.ConnectToServer(target, "MOUSE", null);
        }

        public void CloseConnection(int netIndex)
        {}

        public bool ProcessNetEvent(INetEventProcessor processor)
        {
            throw new NotImplementedException();
        }

        public void Send(int netId, byte[] buff, int length, MessagePriority messagePriority, MessageReliability messageReliability)
        {
            throw new NotImplementedException();
        }

        public void SendLoopback(byte[] buff, int length)
        {
            throw new NotImplementedException();
        }

        public System.Net.IPEndPoint GetEndPointOf(int netId)
        {
            throw new NotImplementedException();
        }

        public System.Net.IPEndPoint EndPoint
        {
            get { throw new NotImplementedException(); }
        }
    }
}
