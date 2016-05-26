using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Lidgren.Network;
using MOUSE.Core;
using NetPeer = Lidgren.Network.NetPeer;

namespace LidgrenWrap
{
    public class LidgrenNetProvider : INetProvider
    {
        readonly ILidgrenEvents _logger;
        readonly NetPeer _peer;
        readonly byte[] _buffer = new byte[1024*1024*10];
        readonly List<NetIncomingMessage> _incomingMessages = new List<NetIncomingMessage>();
        readonly Dictionary<long, LidgrenTransportChannel>  _channels = new Dictionary<long, LidgrenTransportChannel>();

        Action<ITransportChannel> _processIncommingConnection;
        bool _isInitialised = false;
        IPEndPoint _listenEndpoint;

        public LidgrenNetProvider(NetPeerConfiguration config, ILidgrenEvents logger = null)
        {
            _logger = logger;
            _peer = new NetPeer(config);
        }

        public void Dispose()
        {
            _peer.Shutdown("");
        }

        public bool Init(Action<ITransportChannel> incommingConnectionAction)
        {
            if(_isInitialised)
                throw new Exception("LidgrenNetProvider is already initialised");
            _peer.Start();
            _listenEndpoint = new IPEndPoint(_peer.Configuration.LocalAddress, _peer.Port);
            _processIncommingConnection = incommingConnectionAction;
            _isInitialised = true;
            return true;
        }

        public void Connect(IPEndPoint target)
        {
            _logger.ConnectingTo(target);
            _peer.Connect(target);
        }

        public bool PumpEvents()
        {
            _incomingMessages.Clear();
            _peer.ReadMessages(_incomingMessages);

            foreach (NetIncomingMessage incomingMessage in _incomingMessages)
            {
                try
                {
                    LidgrenTransportChannel transportChannel;
                    switch (incomingMessage.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                            _logger.Debug(incomingMessage.ReadString());
                            break;
                        case NetIncomingMessageType.WarningMessage:
                            _logger.Warning(incomingMessage.ReadString());
                            break;
                        case NetIncomingMessageType.ErrorMessage:
                            _logger.Error(incomingMessage.ReadString());
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            var status = (NetConnectionStatus) incomingMessage.ReadByte();
                            switch (status)
                            {
                                case NetConnectionStatus.InitiatedConnect:
                                case NetConnectionStatus.ReceivedInitiation:
                                case NetConnectionStatus.RespondedAwaitingApproval:
                                case NetConnectionStatus.RespondedConnect:
                                case NetConnectionStatus.Disconnecting:
                                    _logger.StatusChanged(status.ToString(), incomingMessage.SenderConnection.RemoteUniqueIdentifier);
                                    break;
                                case NetConnectionStatus.Connected:
                                    _logger.StatusChanged(status.ToString(), incomingMessage.SenderConnection.RemoteUniqueIdentifier);
                                    var channel = new LidgrenTransportChannel(incomingMessage.SenderConnection);
                                    _processIncommingConnection(channel);
                                    _channels.Add(incomingMessage.SenderConnection.RemoteUniqueIdentifier, channel);
                                    break;
                                case NetConnectionStatus.Disconnected:
                                    _logger.StatusChanged(status.ToString(), incomingMessage.SenderConnection.RemoteUniqueIdentifier);
                                    if (_channels.TryGetValue(incomingMessage.SenderConnection.RemoteUniqueIdentifier, out transportChannel))
                                    {
                                        transportChannel.OnDisconnect();
                                        _channels.Remove(incomingMessage.SenderConnection.RemoteUniqueIdentifier);
                                    }
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;

                        case NetIncomingMessageType.Data:
                            int numBytes = incomingMessage.ReadInt32();
                            _logger.OnDataReceived(incomingMessage.SenderConnection.RemoteUniqueIdentifier, numBytes);
                            if (_channels.TryGetValue(incomingMessage.SenderConnection.RemoteUniqueIdentifier, out transportChannel))
                            {

                                incomingMessage.ReadBytes(_buffer, 0, numBytes);
                                transportChannel.OnDataReceived(new ArraySegment<byte>(_buffer, 0, numBytes));
                            }
                            break;

                        default:
                            _logger.Debug("Received " + incomingMessage.MessageType);
                            break;
                    }
                }
                finally
                {
                    _peer.Recycle(incomingMessage);    
                }
            }

            return false;
        }

        public IPEndPoint EndPoint
        {
            get { return _listenEndpoint; }
        }
    }
}
