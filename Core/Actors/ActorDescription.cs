using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace MOUSE.Core.Actors
{
    public class ActorDescription
    {
        public readonly ActorAttribute Attribute;
        public readonly List<NetContractDescription> ImplementedContracts;
        public readonly Type Type;
        public readonly NetContractDescription PrimaryContract;

        public ActorDescription(Type type, IEnumerable<NetContractDescription> contracts, ActorAttribute attribute)
        {
            Attribute = attribute ?? new ActorAttribute();
            ImplementedContracts = contracts.ToList();
            Type = type;

            PrimaryContract = ImplementedContracts.Single(x => x.IsPrimary);
        }

        public override string ToString()
        {
            return string.Format("ActorType<PrimaryContract: {0}, Type: {1}>", PrimaryContract, Type.Name);
        }
    }


    public class ActorProxy<TNetContract>
    {
        public readonly TNetContract Channel;
        public readonly ActorKey Key;

        public ActorProxy(TNetContract channel, ActorKey key)
        {
            Channel = channel;
            Key = key;
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