using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace MOUSE.Core.Actors
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActorAttribute : Attribute
    {
        public Type Coordinator { get; set; }
    }

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

    
}