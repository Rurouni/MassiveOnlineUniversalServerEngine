using System;
using System.IO;
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

    [DataContract]
    public class ActorRemoteInfo
    {
        [DataMember]
        public readonly string Name;
        [DataMember]
        public readonly ActorKey Key;

        public ActorRemoteInfo(string name, ActorKey key)
        {
            Name = name;
            Key = key;
        }

        public override string ToString()
        {
            return string.Format("Actor<Name: {0}, LocalId: {1}, OwnerNodeId:{2}>", Name, Key.LocalActorId, Key.OwnerNodeId);
        }
    }

    public static class ActorRemoteInfoSerializer
    {
        public static void Serialize(ActorRemoteInfo obj, BinaryWriter writer)
        {
            writer.Write(obj.Name);
            writer.Write(obj.Key.OwnerNodeId);
            writer.Write(obj.Key.LocalActorId);
        }

        public static ActorRemoteInfo Deserialize(BinaryReader reader)
        {
            return new ActorRemoteInfo(
                reader.ReadString(),
                new ActorKey(
                   reader.ReadUInt64(),
                   reader.ReadUInt32()));
        }
    }
    
}