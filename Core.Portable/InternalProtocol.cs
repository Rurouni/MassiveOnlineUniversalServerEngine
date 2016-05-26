using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Globalization;
using MOUSE.Core.Actors;

namespace MOUSE.Core
{
    [Serializable]
    [DataContract]
    public sealed class EmptyMessage : Message
    {
    }

    [Serializable]
    [DataContract]
    public sealed class OperationResult : Message
    {
        [DataMember(Order = 1)]
        public ErrorMessage Error { get; set; }

        public bool IsSuccess => Error == null;

        public OperationResult(){}

        public OperationResult(ErrorMessage error)
        {
            Error = error;
        }


        static public OperationResult Success => new OperationResult();

        static public OperationResult Failure(ushort errorCode, string errorString) => new OperationResult(new ErrorMessage(errorCode, errorString));
    }

    [Serializable]
    [DataContract]
    public sealed class ErrorMessage : Message
    {
        [DataMember(Order = 1)]
        public ushort ErrorCode { get; set; }
        [DataMember(Order = 2)]
        public string ErrorString { get; set; }

        public ErrorMessage()
        {
            ErrorCode = 0;
            ErrorString = null;
        }

        public ErrorMessage(ushort errorCode, string errorString)
        {
            ErrorCode = errorCode;
            ErrorString = errorString;
        }
    }

    [Serializable]
    [DataContract]
    public class CallbackChannelRef : IEquatable<CallbackChannelRef>
    {
        [DataMember(Order = 1)]
        readonly byte[] _ip;
        [DataMember(Order = 2)]
        readonly int _port;
        [DataMember(Order = 3)]
        public readonly Guid CallbackChannelId;

        IPEndPoint _address;

        public CallbackChannelRef(IPEndPoint address, Guid callbackChannelId)
        {
            CallbackChannelId = callbackChannelId;
            _ip = address.Address.GetAddressBytes();
            _port = address.Port;
            _address = address;
        }

        public CallbackChannelRef()
        { }

        public IPEndPoint Address
        {
            get
            {
                if (_address == null)
                    _address = new IPEndPoint(new IPAddress(_ip), _port);

                return _address;
            }
        }

        public bool Equals(CallbackChannelRef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _ip.Equals(other._ip) && _port == other._port && CallbackChannelId.Equals(other.CallbackChannelId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (CallbackChannelRef)) return false;
            return Equals((CallbackChannelRef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _ip.GetHashCode();
                hashCode = (hashCode*397) ^ _port;
                hashCode = (hashCode*397) ^ CallbackChannelId.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CallbackChannelRef left, CallbackChannelRef right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CallbackChannelRef left, CallbackChannelRef right)
        {
            return !Equals(left, right);
        }


        public override string ToString()
        {
            return string.Format("{{ CallbackChannelId: {0}, Address:\"{1}\" }}", CallbackChannelId, Address);
        }
    }

    /// <summary>
    /// Notification for NetPeer being disconnected (inherited from Message only for composability of event streams)
    /// Not serializable as intended to be used only for generic message processors
    /// </summary>
    public sealed class PeerDisconnected : Message
    {}

    [Serializable]
    [DataContract]
    public sealed class ActorDisconnected : Message
    {
        [DataMember(Order = 1)]
        public ActorRef Actor { get; set; }

        public ActorDisconnected()
        { }

        public ActorDisconnected(ActorRef actor)
        {
            Actor = actor;
        }
    }

    [Serializable]
    [DataContract]
    public sealed class CallbackChannelDisconnected : Message
    {
        [DataMember(Order = 1)]
        public Guid CallbackChannelId { get; set; }

        public CallbackChannelDisconnected()
        { }

        public CallbackChannelDisconnected(Guid callbackChannelId)
        {
            CallbackChannelId = callbackChannelId;
        }
    }
}
