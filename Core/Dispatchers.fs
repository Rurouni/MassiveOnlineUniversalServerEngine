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

    member x.OnConnected with get() = onConnectedEvent
    member x.OnDisconnected with get() = onDisconnectedEvent
    member x.OnOperation with get() = onOperationEvent
    
    interface IOperationDispatcher with
        member x.Dispatch(operation) = 
            match operation with
            |NodeConnected(context) -> do onConnectedEvent.Trigger(context)
            |NodeDisconnected(context) -> do onDisconnectedEvent.Trigger(context)
            |NodeOperation(operation) -> do onOperationEvent.Trigger(operation)

type ObservableDispatcher() = 
    let onConnectedEvent = new Event<OperationContext>()
    let onDisconnectedEvent = new Event<OperationContext>()
    let onOperationEvent = new Event<IOperation>() 

    let fromEvent (event:IEvent<_,_>) =      
        Observable.Create<_>(fun x ->
            event.Subscribe x.OnNext |> ignore
            new System.Action(fun () -> ()))   

    member x.OnConnected with get():IObservable<OperationContext> =  fromEvent onConnectedEvent
    member x.OnDisconnected with get() = onDisconnectedEvent
    member x.OnOperation with get() = onOperationEvent
    
    interface IOperationDispatcher with
        member x.Dispatch(operation) = 
            match operation with
            |NodeConnected(context) -> do onConnectedEvent.Trigger(context)
            |NodeDisconnected(context) -> do onDisconnectedEvent.Trigger(context)
            |NodeOperation(operation) -> do onOperationEvent.Trigger(operation)
            


