using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using MOUSE.Core;
using NLog;
using Message = MOUSE.Core.Message;
using NetPeer = Lidgren.Network.NetPeer;

namespace LidgrenWrap
{
    public class LidgrenNetProvider : INetProvider
    {
        private readonly Logger Log = LogManager.GetLogger("LidgrenNetProvider");
        private readonly NetPeer _peer;
        private INetChannelConsumer _peerFactory;
        private readonly byte[] _buffer = new byte[1024*1024*10];
        private readonly MemoryStream _stream;
        private readonly BinaryReader _reader;
        private readonly List<NetIncomingMessage> _incomingMessages = new List<NetIncomingMessage>();
        private readonly Dictionary<long, LidgrenNetChannel>  _channels = new Dictionary<long, LidgrenNetChannel>();
        private readonly BufferManager _bufferManager = BufferManager.CreateBufferManager(100000, 1024*1024*10);


        public LidgrenNetProvider(NetPeerConfiguration config)
        {
            _peer = new NetPeer(config);
            _stream = new MemoryStream(_buffer);
            _reader = new BinaryReader(_stream); 
        }

        public bool Init(INetChannelConsumer factory)
        {
            _peer.Start();
            _peerFactory = factory;
            return true;
        }

        public void Shutdown()
        {
            _peer.Shutdown("");
        }

        public void Connect(IPEndPoint target)
        {
            _peer.Connect(target);
        }

        public bool PumpEvents()
        {
            _incomingMessages.Clear();
            _peer.ReadMessages(_incomingMessages);

            foreach (NetIncomingMessage incomingMessage in _incomingMessages)
            {
                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                        Log.Debug(incomingMessage.ReadString());
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        Log.Warn(incomingMessage.ReadString());
                        break;
                    case NetIncomingMessageType.ErrorMessage:
                        Log.Error(incomingMessage.ReadString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        var status = (NetConnectionStatus)incomingMessage.ReadByte();
                        switch (status)
                        {
                            case NetConnectionStatus.InitiatedConnect:
                                break;
                            case NetConnectionStatus.ReceivedInitiation:
                                break;
                            case NetConnectionStatus.RespondedAwaitingApproval:
                                break;
                            case NetConnectionStatus.RespondedConnect:
                                break;
                            case NetConnectionStatus.Connected:
                                var channel = new LidgrenNetChannel(incomingMessage.SenderConnection, _bufferManager);
                                INetChannelListener listener = _peerFactory.OnNetConnect(channel);
                                channel.Listener = listener;
                                _channels.Add(incomingMessage.SenderConnection.RemoteUniqueIdentifier, channel);
                                break;
                            case NetConnectionStatus.Disconnecting:
                                break;
                            case NetConnectionStatus.Disconnected:
                                LidgrenNetChannel netChannel;
                                if (_channels.TryGetValue(incomingMessage.SenderConnection.RemoteUniqueIdentifier, out netChannel))
                                {
                                    netChannel.Listener.OnDisconnected();
                                    _channels.Remove(incomingMessage.SenderConnection.RemoteUniqueIdentifier);
                                    netChannel.Dispose();
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;

                    case NetIncomingMessageType.Data:
                        int numBytes = incomingMessage.ReadInt32();
                        incomingMessage.ReadBytes(_buffer, 0, numBytes);
                        _stream.Position = 0;
                        _channels[incomingMessage.SenderConnection.RemoteUniqueIdentifier].Listener.OnNetData(_reader);
                        break;

                    default:
                        Log.Debug("Received "+ incomingMessage.MessageType);
                        break;
                }
                _peer.Recycle(incomingMessage);
                
            }

            return false;
        }

        public IPEndPoint EndPoint
        {
            get { return new IPEndPoint(_peer.Configuration.LocalAddress, _peer.Configuration.Port);}
        }
    }

    public class LidgrenNetChannel : INetChannel, IDisposable
    {
        private readonly NetConnection _connection;
        private readonly BufferManager _bufferManager;
        private readonly object _globalLock = new object();
        private byte[] _buffer = new byte[1024*1024];
        private BinaryWriter _writer;
        private MemoryStream _stream;

        public LidgrenNetChannel(NetConnection connection, BufferManager bufferManager)
        {
            _connection = connection;
            _bufferManager = bufferManager;
            _buffer = _bufferManager.TakeBuffer(1024*1024);
            _stream = new MemoryStream(_buffer);
            _writer = new BinaryWriter(_stream);
        }

        public uint Id
        {
            get { return (uint)_connection.RemoteUniqueIdentifier; }
        }

        public IPEndPoint EndPoint
        {
            get { return _connection.RemoteEndPoint; }
        }

        public INetChannelListener Listener { get; set; }

        
        public void Send(Message msg)
        {
            NetOutgoingMessage packet = _connection.Peer.CreateMessage();
            lock (_globalLock)
            {
                _stream.Position = 0;
                msg.Serialize(_writer);

                int numBytes = (int) _stream.Position;
                packet.Write(numBytes);
                packet.Write(_buffer, 0, numBytes);
                NetSendResult result = _connection.SendMessage(packet, Convert(msg.Reliability), 0);

                if (result == NetSendResult.FailedNotConnected)
                    throw new Exception("NetSendResult.FailedNotConnected");
            }
        }

        public NetDeliveryMethod Convert(MessageReliability val)
        {
            switch (val)
            {
                case MessageReliability.Unreliable:
                    return NetDeliveryMethod.Unreliable;
                case MessageReliability.UnreliableOrdered:
                    return NetDeliveryMethod.UnreliableSequenced;
                case MessageReliability.Reliable:
                    return NetDeliveryMethod.ReliableUnordered;
                case MessageReliability.ReliableOrdered:
                    return NetDeliveryMethod.ReliableOrdered;
                default:
                    throw new ArgumentOutOfRangeException("val");
            }
        }

        public void Close()
        {
            _connection.Disconnect("");
        }

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _bufferManager.ReturnBuffer(_buffer);
                _buffer = null;
                _writer = null;
                _stream = null;
                _isDisposed = true;
            }
        }

        ~LidgrenNetChannel()
        {
            Dispose();
        }

        
    }
}
