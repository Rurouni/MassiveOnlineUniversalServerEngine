using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;
using ExitGames.Client.Photon;
using System.Net;

namespace PhotonClientWrap
{
    public class PhotonNetClient : INetProvider, IPhotonPeerListener, INetChannel
    {
        PhotonPeer _photon;
        Dictionary<byte, object> _dict = new Dictionary<byte, object>();
        INetChannelConsumer _processor;
        NativeReader _reader = new NativeReader();

        public PhotonNetClient()
        {           
        }

        public bool Startup(INetChannelConsumer processor, IPEndPoint listenEndpoint, int maxConnections)
        {
            _processor = processor;
            _photon = new PhotonPeer(this);
            return true;
        }

        public void Shutdown()
        {
            _photon.Disconnect();
            _photon.StopThread();
        }

        public void Connect(IPEndPoint target)
        {
            _photon.Connect(target.ToString(), "MOUSE");
        }

        public bool PumpEvents()
        {
            _photon.Service();
            return false;
        }

        public IPEndPoint EndPoint
        {
            get 
            {
                return new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], 0);
            }
        }
    
        void IPhotonPeerListener.DebugReturn(DebugLevel level, string message)
        {
            Console.WriteLine(level + ":" + message);
        }

        void IPhotonPeerListener.OnEvent(EventData eventData)
        {
            _reader.SetBuffer((byte[])eventData.Parameters[0], 0);
            _processor.OnNetData(this, _reader);
        }

        void IPhotonPeerListener.OnOperationResponse(OperationResponse operationResponse)
        {
            _reader.SetBuffer((byte[])operationResponse.Parameters[0], 0);
            _processor.OnNetData(this, _reader);
        }

        void IPhotonPeerListener.OnStatusChanged(StatusCode statusCode)
        {
            switch (statusCode)
            {
                case StatusCode.Connect:
                    _processor.OnNetConnectionAccepted(this);
                    break;
                case StatusCode.Disconnect:
                    _processor.OnNetDisconnect(this);
                    break;
                default:
                    break;
            }
        }

        uint INetChannel.Id
        {
            get { return 0; }
        }

        IPEndPoint INetChannel.EndPoint
        {
            get { return new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], 0); }
        }

        void INetChannel.Send(Message msg)
        {
            NativeWriter writer = msg.GetSerialized();
            var arr = new byte[writer.Position];
            Array.Copy(writer.Buff, arr, writer.Position);
            _dict[0] = arr;
            bool isReliable = msg.Reliability == MessageReliability.Reliable || msg.Reliability == MessageReliability.ReliableOrdered;
            _photon.OpCustom(42, _dict, isReliable);
        }

        void INetChannel.Close()
        {
            _photon.Disconnect();
        }
    }
}
