namespace MOUSE.Core

open NLog
open System
open System.Collections.Generic
open System.Linq
open RakNetWrapper

[<AbstractClass>]
type BasicDispatcher() as this = 
    abstract member OnConnected: self:INode * source:INode -> unit
    default x.OnConnected(self, source) = ()

    abstract member OnDisconnected: self:INode * source:INode -> unit
    default x.OnDisconnected(self, source) = ()

    abstract member OnOperation: self:INode * source:INode * IOperation -> unit

    interface IOperationDispatcher with
        member x.Dispatch(operation) = 
            match operation with
            |NodeConnected(context) -> do this.OnConnected(context.Self, context.Source)
            |NodeDisconnected(context) -> do this.OnDisconnected(context.Self, context.Source)
            |NodeOperation(operation) -> do this.OnOperation(operation.Context.Self, operation.Context.Source, operation)


type EventDispatcher() = 
    let onConnectedEvent = new Event<OperationContext>()
    let onDisconnectedEvent = new Event<OperationContext>()
    let onOperationEvent = new Event<IOperation>() 

    [<CLIEvent>]
    member x.OnConnected with get() = onConnectedEvent.Publish
    [<CLIEvent>]
    member x.OnDisconnected with get() = onDisconnectedEvent.Publish
    [<CLIEvent>]
    member x.OnOperation with get() = onOperationEvent.Publish
    
    interface IOperationDispatcher with
        member x.Dispatch(operation) = 
            match operation with
            |NodeConnected(context) -> do onConnectedEvent.Trigger(context)
            |NodeDisconnected(context) -> do onDisconnectedEvent.Trigger(context)
            |NodeOperation(operation) -> do onOperationEvent.Trigger(operation)

type ObservableDispatcher() = 
    let onConnectedEvent = new Subject<OperationContext>()
    let onDisconnectedEvent = new Subject<OperationContext>()
    let onOperationEvent = new Subject<IOperation>() 

    member x.OnConnected with get() =  onConnectedEvent.AsObservable()
    member x.OnDisconnected with get() = onDisconnectedEvent.AsObservable()
    member x.OnOperation with get() = onOperationEvent.AsObservable()
    
    interface IOperationDispatcher with
        member x.Dispatch(operation) = 
            match operation with
            |NodeConnected(context) -> do onConnectedEvent.OnNext(context)
            |NodeDisconnected(context) -> do onDisconnectedEvent.OnNext(context)
            |NodeOperation(operation) -> do onOperationEvent.OnNext(operation)
            


