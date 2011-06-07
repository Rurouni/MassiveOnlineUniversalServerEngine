namespace MOUSE.Core

open System

type WaitReplyInfo = 
    {   OnReply: IOperation -> unit
        OnError: Exception -> unit
        Operation: IOperation
        TargetEntityId: Option<uint64>
        StartTime: DateTime  }

type NodeEntityType(id:uint32, name: string) =
    member x.Id with get() = id
    member x.Name with get() = name


type INodeEntity =
    abstract Create:unit->Async<uint64>
    abstract Delete:uint64->Async<unit>

type NodeEntity(id:uint64, entityType:NodeEntityType, operation) = 
    let mutable _transactionId:uint64 = 0UL
    let mutable _currentOperation:IOperation = operation
    let mutable _waitInfo:option<WaitReplyInfo> = None

    member this.Id with get() = id
    member this.Type with get() = entityType
    member this.TransactionId with get() = _transactionId and set(x) = _transactionId <- x

    member this.SetWaitInfo(waitInfo) = _waitInfo<-waitInfo;
    
    
    
    