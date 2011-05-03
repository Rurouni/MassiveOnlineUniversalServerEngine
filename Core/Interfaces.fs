namespace MOUSE.Core

open NLog
open System
open System.Collections.Generic
open RakNetWrapper

type OperationHeaderType = 
    |BasicHeader = 1
    |EntityHeader = 2

type OperationHeader(operationId:uint64) =
    member x.OperationId with get() = operationId

    static member Write(header:OperationHeader, packet:OutPacket) =
        match header with
        | :? EntityOperationHeader as header ->
            packet.WriteByte(sbyte(OperationHeaderType.EntityHeader))
            packet.WriteUInt64(header.OperationId)
            packet.WriteUInt64(header.SourceEntityId)
            match header.TargetEntityId with
            |Some(id) -> packet.WriteUInt64(id)
            |None -> packet.WriteUInt64(0UL)
            packet.WriteUInt64(header.TransactionId)

        | _ -> 
            packet.WriteByte(sbyte(OperationHeaderType.BasicHeader))
            packet.WriteUInt64(header.OperationId)

    static member Read(packet:InPacket): OperationHeader =
        let headerType = enum<OperationHeaderType>(int32(packet.ReadByte()))
        match headerType with
        | OperationHeaderType.EntityHeader ->
            let operationId = packet.ReadUInt64()
            let sourceEntityId = packet.ReadUInt64()
            let targetEntityId = 
                match packet.ReadUInt64() with
                | 0UL -> None
                | id -> Some(id)
            let transactionId = packet.ReadUInt64()
            new EntityOperationHeader(operationId, sourceEntityId,targetEntityId, transactionId) :> OperationHeader

        | OperationHeaderType.BasicHeader ->
            let operationId = packet.ReadUInt64()
            new OperationHeader(operationId)
        | _ -> failwith ("Unknown header type : " + headerType.ToString())
       

and EntityOperationHeader(operationId:uint64, sourceEntityId:uint64, targetEntityId:option<uint64>, transactionId:uint64) =
    inherit OperationHeader(operationId)
    member x.SourceEntityId with get() = sourceEntityId
    member x.TargetEntityId with get() = targetEntityId
    member x.TransactionId with get() = transactionId


type IOperation =
    abstract Header : OperationHeader with get, set
    abstract Context : OperationContext with get, set
    abstract Description : OperationDescription with get

and INode = 
    abstract Id : uint64 with get
    abstract Execute: IOperation -> unit
    abstract Connect: string * uint16 -> unit

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

type IOperationDispatcher = 
    abstract Dispatch: NodeEvent -> unit
