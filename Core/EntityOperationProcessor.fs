namespace MOUSE.Core

open NLog
open System
open System.Collections.Generic
open System.Linq
open RakNetWrapper
open DictionaryExtensions

type EntityOperationDispatcher(hostNode:INode, entityRepository:NodeEntityRepository) = 
    let Log = LogManager.GetCurrentClassLogger()
    let entitiesOnNodes = new Dictionary<uint64, INode>()
    let entityTypeCoordinatorsOnNodes = new Dictionary<uint32, INode>()//by entityType

    let onConnected(context:OperationContext) =
        Log.Debug("{0} has connected", context.Source)
    let onDisconnected(context:OperationContext) =
        Log.Debug("{0} has disconnected", context.Source)
    let onOperation(operation:IOperation) =
        Log.Debug("Received {0} from {1}", operation, operation.Context.Source)

    let findNode(operation:IOperation, targetEntityId:option<uint64>):INode =
        match operation.Description.TargetType with
        |Type -> ()
        |Instance -> 
            let targetEntityId = targetEntityId.Value
            operation.Header <- new EntityInstanceOperationHeader(operation.Description.OperationId, source.Id, source.TransactionId, targetEntityId)
            match  entityRepository.TryFind(targetEntityId) with //we have it locally?
            |Some(entity) -> hostNode
            |None -> 
                match hostNode.Type with
                |NodeType.Client -> raise (new NotImplementedException())
                |NodeType.Master-> 
                    match entitiesOnNodes.TryFind(targetEntityId) with  //exists in cluster?
                    |Some(node) -> node
                    |None->
                        let entityOpDesc = operation.Description :?> EntityOperationDescription
                        match entityTypeMastersOnNodes.TryFind(entityOpDesc.EntityType)  with
                        |Some(node)->node
                        |None -> hostNode.CoordinatorNode
                |x ->failwith "Not supported node type-"+ (x.ToString())
        |x ->failwith "Not supported operation target type-"+ (x.ToString())

    member this.Execute(source:NodeEntity, operation:IOperation, ?targetEntityId:uint64) =
        async{
            let targetNode = findNode(operation.Description.EntityType, targetEntityId)
            match operation.Description.TargetType with
            |Type -> ()
            |Instance -> 
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
   
    
    interface INodeEventProcessor with
        member x.Process(operation) = 
            match operation with
            |NodeConnected(context) -> do onConnected(context)
            |NodeDisconnected(context) -> do onDisconnected(context)
            |NodeOperation(operation) -> do onOperation(operation)
                
                    

