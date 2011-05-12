namespace MOUSE.Core

open RakNetWrapper
open System.Threading
open System
open System.Collections.Generic
open NLog
open DictionaryExtensions
open NodeInternalProtocol



type NodeCommand = 
    |Connect of string * uint16 * AsyncReplyChannel<ConnectionAttemptResult>
    |Disconnect of NetId
    |ReceiveMessages
    |Send of NetId * IOperation
    |LoopbackSend of IOperation


type Node(nodeType : NodeType, domainProtocol : IProtocolDescription, processor : INodeEventProcessor,
          selfIp, selfPort, maxConnections, maxMessagesPerTick, sleepTimeMs) as this = class
    
    let Log = LogManager.GetCurrentClassLogger()
    
    
    let netPeer = new RakPeerInterface()
    let startUpResult = netPeer.Startup(selfIp, selfPort, maxConnections, 30000)
    let channels = new Dictionary<uint64, INode>()
    let description = { Id = netPeer.Id().Id
                        Type = nodeType
                        Ip = selfIp
                        Port = selfPort }

    let send(netId, operation: IOperation) = 
        use packet = new OutPacket();
        packet.WriteUByte(byte(RakNetMessages.ID_USER_PACKET_ENUM))
        operation.Serialize(packet)
        netPeer.Send(netId, packet,
            enum<MessagePriority>(int32(operation.Description.Priority)),
            enum<MessageReliability>(int32(operation.Description.Reliability)), sbyte(0), false) |> ignore


    let onNodeIntroductionRequest(netId:NetId, nodeDescription:NodeDescription) =
        let connectedNode = new NodeProxy( netId, this, nodeDescription.Type)
        processor.Process(NodeConnected(new OperationContext(this, connectedNode)))
        channels.Add( nodeDescription.Id, connectedNode)
        send(netId, new NodeIntroductionReply(description, list.Empty))

    let onNodeIntroductionReply(netId:NetId, masterDescription:NodeDescription, nodes:list<NodeDescription>) =
        ()
        
    let processOperation(msg:InPacket) = 
        let header = OperationHeader.Read(msg)
        match read header.OperationId msg with
        |NodeIntroductionRequest(desc) -> onNodeIntroductionRequest(msg.SenderNetId, desc)
        |NodeIntroductionReply(master, nodes) -> onNodeIntroductionReply(msg.SenderNetId,  master, nodes)
        |None ->
            match DictionaryExtensions.tryFind channels senderId with
            |None -> Log.Error("Received operationId:{0} from unregistered Node<Id:{1}>", header.OperationId, senderId)
            |Some(sourceNode) -> 
                if domainProtocol.Contains header.OperationId then
                    let operation = protocol.Deserialize(header.OperationId, msg)
                    operation.Header <- header
                    operation.Context <- new OperationContext(this, sourceNode)
                    processor.Process(NodeOperation(operation))
                else Log.Error("Received unknown operationId:{0} from Node<Id:{1}>", header.OperationId, sourceNode) 

    let processNetMessage(netMsgId:RakNetMessages, msg:InPacket, senderId:uint64) =
        match netMsgId with
        | RakNetMessages.ID_NEW_INCOMING_CONNECTION -> ()//remove unintroduced nodes later
        | RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED ->
            use packet = new OutPacket()
            write packet (NodeIntroductionRequest(description))
            

        | RakNetMessages.ID_DISCONNECTION_NOTIFICATION
        | RakNetMessages.ID_DETECT_LOST_CONNECTIONS
        | RakNetMessages.ID_CONNECTION_LOST
        | RakNetMessages.ID_CONNECTION_REQUEST_ACCEPTED ->
            match DictionaryExtensions.tryFind channels senderId with
            |None -> 
                Log.Warn("Received disconnection event:{0} from unregistered Node<Id:{1}>", netMsgId, senderId)
            |Some(disconnectedNode) ->
                processor.Process(NodeDisconnected(new OperationContext(this, disconnectedNode)))
                channels.Remove(msg.SenderNetId.Id) |> ignore

        | RakNetMessages.ID_USER_PACKET_ENUM -> processOperation(msg)
                
        | _ -> Log.Warn("Unhandled MessageType:{0} from Node<Id:{1}>", netMsgId, msg.SenderNetId.Id)

    let rec loop(inbox:MailboxProcessor<NodeCommand>) = 
        

        let rec receiveMessages(counter)  = 
            if counter > 0 then
                use msg = netPeer.Receive()
                if msg <> null then
                    let senderId = msg.SenderNetId.Id
                    let netMsgId = enum<RakNetMessages>(int32(msg.ReadUByte()))
                    Log.Trace("Received {0} from Node<Id:{1}>", netMsgId, senderId)
                    processNetMessage(netMsgId, msg, senderId)
                    receiveMessages(counter - 1)
            
        async{
            while true do
                try   
                    let! command = inbox.Receive()
                    match command with
                    |Connect(ip, port, replyChannel) -> replyChannel.Reply(netPeer.Connect(ip, port))
                    |Disconnect(netId) -> netPeer.CloseConnection(netId, true, byte(0))
                    |Send(netId, operation) -> 
                        send(netId, operation)
                    |LoopbackSend(operation)-> processor.Process(NodeOperation(operation))
                    |ReceiveMessages ->
                        receiveMessages(maxMessagesPerTick)
                        do! Async.Sleep(sleepTimeMs)
                        inbox.Post(ReceiveMessages)  
                with 
                | ex -> 
                    Log.Error("Unhandled exception in main node loop -"+ ex.ToString())
 
                
        }

    
    let agent = new MailboxProcessor<NodeCommand>(loop)
    //let mutable _onMessage       = fun (node:Node) (msg:IMessage)    -> Log.Info("OnMessage Id:{0}", msg.Id)
    //let mutable _onChannelOpened = fun (node:Node) (address:SystemAddress) -> Log.Info("OnChannelOpened Id:{0}", address.GUID)
    //let mutable _onChannelClosed = fun (node:Node) (address:SystemAddress) -> Log.Info("OnChannelClosed Id:{0}", address.GUID)

    //member this.OnMessage       with get() = _onMessage and set(func) = _onMessage       <-func 
    //member this.OnChannelOpened with get() = _onMessage and set(func) = _onChannelOpened <-func 
    //member this.OnChannelClosed with get() = _onMessage and set(func) = _onChannelClosed <-func 
    member this.Start() =
        Log.Info("Node<Id:{0}> has started on {1}:{2}", this.Id, selfIp, selfPort)
        agent.Start()
        agent.Post(ReceiveMessages)

    member this.SendTo(netId: NetId, operation:IOperation) =
        agent.Post(Send(netId, operation))

    member this.SendLoopback(operation:IOperation) =
        agent.Post(LoopbackSend(operation))

    member this.Connect(host, port) = 
        let result = agent.PostAndReply(fun replyChannel -> Connect(host, port, replyChannel))
        match result with
        |ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED -> ()
        |code -> Log.Error("Connection error: {0}", code)
    
    member this.Id with get() = netPeer.Id().Id;
    member this.Type with get() = nodeType

    interface INode with
        member x.Id with get() = this.Id;
        member x.Type with get() = this.Type
        member x.Connect(host, port) = this.Connect(host, port)
        member x.Execute(operation) = this.SendLoopback(operation)
        
end
and NodeProxy(netId:NetId, owner:Node, nodeType:NodeType, ?master:INode) = 
    interface INode with
        member x.Id with get():uint64 = netId.Id
        member x.Type with get() = nodeType
        member x.Execute(operation:IOperation) = owner.SendTo(netId, operation)
        member x.Connect(host, port) = failwith "This is only projection of connected node, so this operation is useless"



