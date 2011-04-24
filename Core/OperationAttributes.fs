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

type OperationDescription = {
    Priority : OperationPriority
    Reliability : OperationReliability
}



type OperationResultCode = 
    |Ok = 200
    |Failed_NotConnected = 400
    |Failed_EntityNotFound = 404
    |Failed_NotProcessed = 500

type OperationTargetType = 
    |Any = 1
    |Instance = 2
    |All = 3

[<AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)>]
type NodeEntityContractAttribute() = 
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)>]
type NodeEntityOperationAttribute() = 
    inherit Attribute()

    let mutable _targetType = OperationTargetType.Instance
    let mutable _isReliable = false
    let mutable _isOrdered = false
    let mutable _additionalResultCode:Type = null

    member this.Target               with get() = _targetType            and set(x) = do _targetType<-x
    member this.IsReliable           with get() = _isReliable            and set(x) = do _isReliable<-x 
    member this.IsOrdered            with get() = _isOrdered             and set(x) = do _isOrdered<-x
    member this.AdditionalResultCode with get() = _additionalResultCode  and set(x) = do _additionalResultCode<-x

