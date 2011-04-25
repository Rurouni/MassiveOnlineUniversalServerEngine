namespace SampleServer

open MOUSE.Core
open Protocol
open Protocol.Generated
open NLog

type PingPongServer()= 
    inherit EventDispatcher()
    
    let Log = LogManager.GetCurrentClassLogger()

    member this.Init() = 
        this.OnConnected.Add(fun context -> Log.Info("Node<Id:{0}> has connected to us", context.Source.Id))
        this.OnDisconnected.Add(fun context -> Log.Info("Node<Id:{0}> has disconnected from us", context.Source.Id))
        this.OnOperation.Add(fun operation ->
            match operation with
            | :? Ping as ping ->
                Log.Info("Received Ping<RequestId:{0}>", ping.RequestId)
                operation.Context.Source.Execute( new Pong(ping.RequestId))
            | _ -> ()
        )
        


