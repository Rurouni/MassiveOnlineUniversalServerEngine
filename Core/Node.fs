namespace MOUSE.Core

open RakNetWrapper
open System.Threading
open System
open System.Collections.Generic
open NLog
open DictionaryExtensions

type NodeType = 
    |Master = 1
    |Server = 2
    |Client = 3

type NodeCommand = 
    |Connect of string * uint16 * AsyncReplyChannel<ConnectionAttemptResult>
    |Disconnect of NetId
    |ReceiveMessages
    |Send of NetId * OutPacket * OperationPriority * OperationReliability
    |LoopbackSend of IOperation


type Node(nodeType : NodeType, protocol : IProtocolDescription, dispatcher : IOperationDispatcher,
          selfIp, selfPort, maxConnections, maxMessagesPerTick, sleepTimeMs) as this = class
    
    let Log = LogManager.GetCurrentClassLogger()
    
    let netPeer = new RakPeerInterface()
    let startUpResult = netPeer.Startup(selfIp, selfPort, maxConnections, 30000)

    let rec nodeLoop = new MailboxProcessor<NodeCommand>(fun inbox ->
        let channels = new Dictionary<uint64, INode>()
        let rec receiveMessages(counter)  = 
            if(counter > 0) then
                use msg = netPeer.Receive()
                if msg <> null then
                    let sourceNodeId = msg.SenderNetId.Id
                    let netMsgId = enum<RakNetMessages>(int32(msg.ReadByte()))
                    Log.Trace("Received {0} from Node<Id:{1}>", netMsgId, sourceNodeId)

                    match netMsgId with
                    | RakNetMessages.ID_NEW_INCOMING_CONNECTION
                    | RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED ->
                        let connectedNode = new NodeProxy( msg.SenderNetId, this)
                        do dispatcher.Dispatch(NodeConnected(new OperationContext(this, connectedNode)))
                        do channels.Add( msg.SenderNetId.Id, connectedNode)

                    | RakNetMessages.ID_DISCONNECTION_NOTIFICATION
                    | RakNetMessages.ID_DETECT_LOST_CONNECTIONS
                    | RakNetMessages.ID_CONNECTION_LOST
                    | RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED ->
                        match DictionaryExtensions.tryFind channels sourceNodeId with
                        |None -> Log.Error("Received disconnection event:{0} from unregistered Node<Id:{1}>", netMsgId, sourceNodeId)
                        |Some(disconnectedNode) ->
                            do dispatcher.Dispatch(NodeDisconnected(new OperationContext(this, disconnectedNode)))
                            do channels.Remove(msg.SenderNetId.Id) |> ignore

                    | RakNetMessages.ID_USER_PACKET_ENUM -> 
                        let header = OperationHeader.Read(msg)
                        match DictionaryExtensions.tryFind channels sourceNodeId with
                        |None -> Log.Error("Received operationId:{0} from unregistered Node<Id:{1}>", header.OperationId, sourceNodeId)
                        |Some(sourceNode) ->
                            if protocol.Contains(header.OperationId) then 
                                let operation = protocol.Deserialize(header.OperationId, msg)
                                do operation.Header = header
                                do operation.Context = new OperationContext(this, sourceNode)
                                do dispatcher.Dispatch(NodeOperation(operation))
                            else Log.Error("Received unknown operationId:{0} from Node<Id:{1}>", header.OperationId, sourceNode)
                    | _ -> Log.Warn("Unhandled MessageType:{0} from Node<Id:{1}>", netMsgId, msg.SenderNetId.Id)
                    receiveMessages(counter - 1)

        async{  
            while true do
                let! command = inbox.Receive()
                match command with
                |Connect(ip, port, replyChannel) -> replyChannel.Reply(netPeer.Connect(ip, port))
                |Disconnect(netId) -> netPeer.CloseConnection(netId, true, byte(0))
                |Send(netId, packet, priority, reliability) -> 
                    netPeer.Send(netId, packet,
                        enum<MessagePriority>(int32(priority)),
                        enum<MessageReliability>(int32(reliability)), sbyte(0), false) |> ignore
                |LoopbackSend(operation)-> dispatcher.Dispatch(NodeOperation(operation))
                |ReceiveMessages ->
                    do receiveMessages(maxMessagesPerTick)
                    do! Async.Sleep(sleepTimeMs)
                    do inbox.Post(ReceiveMessages)  
            })

    
    
    //let mutable _onMessage       = fun (node:Node) (msg:IMessage)    -> Log.Info("OnMessage Id:{0}", msg.Id)
    //let mutable _onChannelOpened = fun (node:Node) (address:SystemAddress) -> Log.Info("OnChannelOpened Id:{0}", address.GUID)
    //let mutable _onChannelClosed = fun (node:Node) (address:SystemAddress) -> Log.Info("OnChannelClosed Id:{0}", address.GUID)

    //member this.OnMessage       with get() = _onMessage and set(func) = _onMessage       <-func 
    //member this.OnChannelOpened with get() = _onMessage and set(func) = _onChannelOpened <-func 
    //member this.OnChannelClosed with get() = _onMessage and set(func) = _onChannelClosed <-func 
    member this.Start() =
            nodeLoop.Start()

    member this.SendTo(netId: NetId, operation:IOperation) =
        if protocol.Contains(operation.Header.OperationId) then
            let packet = protocol.Serialize operation
            nodeLoop.Post(Send(netId, packet, operation.Description.Priority, operation.Description.Reliability))
        else failwith("Cant serialize operationId:" + operation.Header.OperationId.ToString())

    member this.SendLoopback(operation:IOperation) =
        nodeLoop.Post(LoopbackSend(operation))

    member this.Connect(host, port) = 
        let result = nodeLoop.PostAndReply(fun replyChannel -> Connect(host, port, replyChannel))
        match result with
        |ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED -> ()
        |code -> Log.Error("Connection error: {0}", code)

    interface INode with
        member x.Id with get():uint64 = netPeer.Id().Id;
        member x.Connect(host, port) = this.Connect(host, port)
        member x.Execute(operation) = this.SendLoopback(operation)
end
and NodeProxy(netId:NetId, owner:Node) = 
    interface INode with
        member x.Id with get():uint64 = netId.Id
        member x.Execute(operation:IOperation) = owner.SendTo(netId, operation)
        member x.Connect(host, port) = failwith "This is only projection of connected node, so this operation is useless"
