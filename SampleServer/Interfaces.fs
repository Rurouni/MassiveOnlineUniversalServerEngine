namespace SampleDomain.Interfaces
open MOUSE.Core

[<NodeEntityContractAttribute>]
type public IPinger = 
    [<NodeEntityOperationAttribute(Target = OperationTargetType.Any,
         Reliability = OperationReliability.ReliableOrdered, 
         Priority = OperationPriority.Medium)>]
    abstract Ping: requestId:int32 -> Async<int32>

