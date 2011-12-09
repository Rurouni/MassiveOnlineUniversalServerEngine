﻿using System;
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
        private PhotonPeer _photon;
        private INetChannelConsumer _channelFactory;
        private INetChannelListener _channelListener;
        private NativeReader _reader = new NativeReader();
        private string _applicationName;
        private IPEndPoint _serverEndPoint;
        

        public PhotonNetClient(string applicationName)
        {
            _applicationName = applicationName;
        }

        public bool Init(INetChannelConsumer factory)
        {
            _channelFactory = factory;
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
            _serverEndPoint = target;
            bool connected = _photon.Connect(target.ToString(), _applicationName);
            if(!connected)
                throw new Exception("Photon Client can't connect to " + target);
            //if(connected)
            //    _channelListener = _channelFactory.OnNetConnect(this);
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
            _channelListener.OnNetData(_reader);
        }

        void IPhotonPeerListener.OnOperationResponse(OperationResponse operationResponse)
        {
            _reader.SetBuffer((byte[])operationResponse.Parameters[0], 0);
            _channelListener.OnNetData(_reader);
        }

        void IPhotonPeerListener.OnStatusChanged(StatusCode statusCode)
        {
            
            switch (statusCode)
            {
                case StatusCode.Connect:
                    _channelListener = _channelFactory.OnNetConnect(this);
                    break;
                case StatusCode.Disconnect:
                    if (_channelListener != null)
                        _channelListener.OnDisconnected();
                    
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
            get { return _serverEndPoint; }
        }

        void INetChannel.Send(Message msg)
        {
            NativeWriter writer = msg.GetSerialized();
            var arr = new byte[writer.Position];
            Array.Copy(writer.Buff, arr, writer.Position);

            var data = new Dictionary<byte, object>();
            data[0] = arr;
            bool isReliable = msg.Reliability == MessageReliability.Reliable || msg.Reliability == MessageReliability.ReliableOrdered;
            _photon.OpCustom(42, data, isReliable);
        }

        void INetChannel.Close()
        {
            _photon.Disconnect();
        }
    }
}