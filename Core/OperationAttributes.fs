namespace MOUSE.Core

open System

//need to be in sync with net lib used
type OperationReliability =
    |Unreliable = 0
    |UnreliableOrdered = 1
    |ReliableUnordered = 2
    |ReliableOrdered = 3
    |ReliableSequenced = 4

//need to be in sync with net lib used
type OperationPriority =
    |Immediate = 0
    |High = 1
    |Medium = 2
    |Low = 3

type OperationResultCode = 
    |Ok = 200
    |Failed_NotConnected = 400
    |Failed_EntityNotFound = 404
    |Failed_NotProcessed = 500


type OperationDescription(operationId : uint64, priority : OperationPriority, reliability : OperationReliability) =
    member x.OperationId with get() = operationId
    member x.Priority with get() = priority
    member x.Reliability with get() = reliability
    
type EntityOperationType = 
    |Global = 1
    |Instance = 2

type EntityOperationDescription(operationId, priority, reliability, operationType : EntityOperationType) =
    inherit OperationDescription(operationId, priority, reliability)

    member x.OperationType with get() = operationType


[<AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)>]
type NodeEntityContractAttribute() = 
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)>]
type NodeEntityOperationAttribute() = 
    inherit Attribute()

    let mutable _targetType = EntityOperationType.Instance
    let mutable _reliability = OperationReliability.ReliableOrdered
    let mutable _priority = OperationPriority.Medium
    let mutable _additionalResultCode:Type = null

    member this.Target               with get() = _targetType            and set(x) = do _targetType<-x
    member this.Reliability          with get() = _reliability           and set(x) = do _reliability<-x 
    member this.Priority             with get() = _priority              and set(x) = do _priority<-x
    member this.AdditionalResultCode with get() = _additionalResultCode  and set(x) = do _additionalResultCode<-x

