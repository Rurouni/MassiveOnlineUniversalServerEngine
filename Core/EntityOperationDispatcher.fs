namespace MOUSE.Core

open NLog
open System
open System.Collections.Generic
open System.Linq
open RakNetWrapper

type EntityOperationDispatcher(node:INode, entityRepository:NodeEntityRepository) = 
    let Log = LogManager.GetCurrentClassLogger()
    let routingTable = new Dictionary<uint64, INode>()

    let onConnected(context:OperationContext) =
        Log.Debug("{0} has connected", context.Source)
    let onDisconnected(context:OperationContext) =
        Log.Debug("{0} has disconnected", context.Source)
    let onOperation(operation:IOperation) =
        Log.Debug("Received {0} from {1}", operation, operation.Context.Source)

    let findNode(enityType, ?targetEntityId:uint64):Async<INode> =
        async{
            match targetEntityId with
            |Some(id) -> findHostNode(entityType, id)
            |None -> findAny
        }

    member this.Execute(source:NodeEntity, operation:IOperation, ?targetEntityId:uint64) =
        async{
            match operation.Description.Target with
            |
            let! targetNode = findNode(operation.Description.EntityType, targetEntityId)
            operation.Header <- new EntityOperationHeader(operation.Description.OperationId, source.Id, targetEntityId, source.TransactionId)
            do targetNode.Execute(operation)
        }

    member this.ExecuteAndWaitReply<'TReply, 'TResult when 'TReply:>IOperation>(source:NodeEntity, operation:IOperation, mapFunc:'TReply->'TResult, ?targetEntityId:uint64) =
        async{
            do! this.Execute(source, operation, targetEntityId)
            let! reply = Async.FromContinuations(fun (cont, econt, ccont) ->
                let waitInfo = 
                    {   OnReply = cont
                        OnError = econt
                        Operation = operation
                        TargetEntityId = targetEntityId
                        StartTime = DateTime.Now    }
                do source.SetWaitInfo(Some(waitInfo)))
            do source.SetWaitInfo(None)
            return mapFunc(reply:?>'TReply)
        }
   
    
    interface IOperationDispatcher with
        member x.Dispatch(operation) = 
            match operation with
            |NodeConnected(context) -> do onConnected(context)
            |NodeDisconnected(context) -> do onDisconnected(context)
            |NodeOperation(operation) -> do onOperation(operation)
                
                    

