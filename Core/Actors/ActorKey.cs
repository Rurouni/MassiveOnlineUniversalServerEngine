using System;
using System.Runtime.Serialization;

namespace MOUSE.Core.Actors
{
    [DataContract]
    public struct ActorKey : IEquatable<ActorKey>
    {
        [DataMember]
        public readonly ulong OwnerNodeId;
        [DataMember]
        public readonly uint LocalActorId;

        public ActorKey(ulong ownerNodeId, uint localActorId)
        {
            OwnerNodeId = ownerNodeId;
            LocalActorId = localActorId;
        }

        public bool Equals(ActorKey other)
        {
            return OwnerNodeId == other.OwnerNodeId && LocalActorId == other.LocalActorId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActorKey && Equals((ActorKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (OwnerNodeId.GetHashCode()*397) ^ (int) LocalActorId;
            }
        }
    }

    public struct ActorProxyKey : IEquatable<ActorProxyKey>
    {
        public readonly ActorKey ActorKey;
        public readonly uint NetContractId;

        public ActorProxyKey(ActorKey actorKey, uint netContractId)
        {
            ActorKey = actorKey;
            NetContractId = netContractId;
        }

        public bool Equals(ActorProxyKey other)
        {
            return ActorKey.Equals(other.ActorKey) && NetContractId == other.NetContractId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActorProxyKey && Equals((ActorProxyKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ActorKey.GetHashCode()*397) ^ (int) NetContractId;
            }
        }
    }
    
}