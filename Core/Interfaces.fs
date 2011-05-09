namespace MOUSE.Core

open NLog
open System
open System.Collections.Generic
open RakNetWrapper

type OperationHeaderType = 
    |Basic = 1
    |EntityTypeOperation = 2
    |EntityInstanceOperation = 3

type NodeType = 
    |Master = 1
    |Client = 2

type OperationHeader(operationId:uint64) =
    new(packet:InPacket) = new OperationHeader(packet.ReadUInt64())

    member x.OperationId with get() = operationId

    abstract member HeaderType: OperationHeaderType with get
    default x.HeaderType with get() = OperationHeaderType.Basic

    abstract member Write: OutPacket ->unit
    default x.Write(packet:OutPacket) = 
        packet.WriteUInt64(x.OperationId)

    static member Write(header:OperationHeader, packet:OutPacket) =
        packet.WriteByte(sbyte(header.HeaderType))
        header.Write(packet)

    static member Read(packet:InPacket): OperationHeader =
        let headerType = enum<OperationHeaderType>(int32(packet.ReadByte()))
        match headerType with
        | OperationHeaderType.EntityInstanceOperation ->
            new EntityInstanceOperationHeader(packet) :> OperationHeader
        | OperationHeaderType.EntityTypeOperation ->
            new EntityTypeOperationHeader(packet) :> OperationHeader
        | OperationHeaderType.Basic ->
            new OperationHeader(packet)
        | _ -> failwith ("Unknown header type : " + headerType.ToString())
       

and EntityTypeOperationHeader =
    inherit OperationHeader
    val sourceEntityId:uint64
    val transactionId:uint64

    new (operationId:uint64, sourceEntityId, transactionId) = {
        inherit OperationHeader(operationId)
        sourceEntityId = sourceEntityId
        transactionId = transactionId 
    }

    new (packet :InPacket) = {
        inherit OperationHeader(packet);
        sourceEntityId = packet.ReadUInt64()
        transactionId = packet.ReadUInt64() 
    }
    
    override x.Write(packet:OutPacket) = 
        packet.WriteUInt64(x.sourceEntityId)
        packet.WriteUInt64(x.sourceEntityId)
    override x.HeaderType with get() = OperationHeaderType.EntityTypeOperation
    member x.SourceEntityId with get() = x.sourceEntityId
    member x.TransactionId with get() = x.transactionId

and EntityInstanceOperationHeader =
    inherit EntityTypeOperationHeader
    val targetEntityId:uint64

    new (operationId, sourceEntityId, transactionId, targetEntityId) = {
        inherit EntityTypeOperationHeader(operationId, sourceEntityId, transactionId)
        targetEntityId = targetEntityId
    }

    new (packet :InPacket) = {
        inherit EntityTypeOperationHeader(packet);
        targetEntityId = packet.ReadUInt64()
    }

    override x.HeaderType with get() = OperationHeaderType.EntityInstanceOperation
    member x.TargetEntityId with get() = x.targetEntityId


type IOperation =
    abstract Header : OperationHeader with get, set
    abstract Context : OperationContext with get, set
    abstract Description : OperationDescription with get

and INode = 
    abstract Id : uint64 with get
    abstract Type: NodeType with get
    abstract Execute: IOperation -> unit
    abstract Connect: string * uint16 -> Async<unit>

and OperationContext(self:INode, source:INode) = 
    member this.Source with get() = source
    member this.Self with get() = self

type NodeEvent =
    |NodeConnected of OperationContext
    |NodeDisconnected of OperationContext
    |NodeOperation of IOperation

type IProtocolDescription =
    abstract Deserialize: operationId:uint64 * operationData:InPacket -> IOperation
    abstract Serialize : IOperation * OutPacket -> unit
    abstract Contains:  operationId:uint64 -> bool

type INodeEventProcessor = 
    abstract Process: NodeEvent -> unit
