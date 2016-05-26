using System.Collections.Generic;
using System.Fabric;
using Newtonsoft.Json;

namespace MOUSE.Core.Azure.ServiceFabric
{
    public class ServiceFabricHelpers
    {
        static public Dictionary<string, string> GetEnvironmentProperties(ServiceContext context)
        {
            return new Dictionary<string, string>()
            {
                {"nodeName", context.NodeContext.NodeName},
                {"serviceTypeName", context.ServiceTypeName},
                {"serviceName", context.ServiceName.ToString()},
                {"partitionId", context.PartitionId.ToString()},
                {"replicaOrInstanceId", context.ReplicaOrInstanceId.ToString()}
            };
        }
    }
}