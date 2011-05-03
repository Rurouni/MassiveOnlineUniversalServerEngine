namespace MOUSE.Core

open System

type WaitReplyInfo = 
    {   OnReply: IOperation -> unit
        OnError: Exception -> unit
        Operation: IOperation
        TargetEntityId: Option<uint64>
        StartTime: DateTime  }

type NodeEntity(id:uint64, operation) = 
    let mutable _transactionId:uint64 = 0UL
    let mutable _currentOperation:IOperation = operation
    let mutable _waitInfo:Option<WaitReplyInfo> = None

    member this.Id with get() = id
    member this.TransactionId with get() = _transactionId and set(x) = _transactionId <- x

    member this.SetWaitInfo(waitInfo) = _waitInfo<-waitInfo;
    
    
    
    