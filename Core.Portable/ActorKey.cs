using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

namespace MOUSE.Core.Actors
{
    [DataContract]
    public struct ActorKey : IEquatable<ActorKey>
    {
        [DataMember(Order = 1)]
        public readonly string Id;

        public ActorKey(string actorId)
        {
            Id = actorId;
        }

        public bool Equals(ActorKey other)
        {
            return string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActorKey && Equals((ActorKey) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        static public bool operator ==(ActorKey left, ActorKey right)
        {
            return left.Equals(right);
        }

        static public bool operator !=(ActorKey left, ActorKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{{ Id: {Id} }}";
        }
    }

    [DataContract]
    public struct ActorRef : IEquatable<ActorRef>
    {
        [DataMember(Order = 1)]
        readonly byte[] _locationIp;
        [DataMember(Order = 2)]
        readonly int _locationPort;
        [DataMember(Order = 3)]
        public readonly ActorKey Key;

        IPEndPoint _location;

        
        public ActorRef(IPEndPoint location,  ActorKey key)
        {
            Key = key;
            _locationIp = location.Address.GetAddressBytes();
            _locationPort = location.Port;
            _location = location;
        }

        public IPEndPoint Location
        {
            get
            {
                if (_location == null)
                    _location = new IPEndPoint(new IPAddress(_locationIp), _locationPort);

                return _location;
            }
        }


        public bool Equals(ActorRef other)
        {
            return _locationIp.Equals(other._locationIp) && _locationPort == other._locationPort && Key.Equals(other.Key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActorRef && Equals((ActorRef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _locationIp.GetHashCode();
                hashCode = (hashCode*397) ^ _locationPort;
                hashCode = (hashCode*397) ^ Key.GetHashCode();
                return hashCode;
            }
        }

        static public bool operator ==(ActorRef left, ActorRef right)
        {
            return left.Equals(right);
        }

        static public bool operator !=(ActorRef left, ActorRef right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{{ Key: {Key}, Location:\"{Location}\" }}";
        }
    }
   
}