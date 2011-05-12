namespace MOUSE.Core

open RakNetWrapper
open System
open System.Collections.Generic
open DictionaryExtensions

type NodeDescription = {
    Id: uint64
    Type: NodeType
    Ip: string
    Port: uint16
}
type NodeOperationIds = 
    |NodeIntroductionRequestId = 1
    |NodeIntroductionReplyId = 2

type NodeIntroductionRequest

type NodeOperation = 
    |NodeIntroductionRequest of NodeDescription
    |NodeIntroductionReply of NodeDescription * list<NodeDescription> //master*connectionPropagation

[<AutoOpen>]
module NodeInternalProtocol =
    let nodeIntroductionRequestHeader = new OperationHeader(uint64(NodeOperationIds.NodeIntroductionRequestId))
    let nodeIntroductionReplyHeader = new OperationHeader(uint64(NodeOperationIds.NodeIntroductionRequestId))

    let writeNodeDesc (packet:OutPacket) (desc:NodeDescription)  =
        packet.WriteUInt64(desc.Id)
        packet.WriteByte(sbyte(desc.Type))
        packet.WriteString(desc.Ip)
        packet.WriteUInt16(desc.Port)

    let readNodeDesc (packet:InPacket) = 
        {   Id = packet.ReadUInt64()
            Type = enum<NodeType>(int32(packet.ReadByte()))
            Ip = packet.ReadString()
            Port = packet.ReadUInt16()  }
        
    let write (packet:OutPacket) operation  = 
        match operation with 
        |NodeIntroductionRequest(desc) ->
            OperationHeader.Write(nodeIntroductionRequestHeader, packet)
            writeNodeDesc packet desc
        |NodeIntroductionReply(master, nodes) ->
            OperationHeader.Write(nodeIntroductionReplyHeader, packet)
            writeNodeDesc packet master
            packet.WriteInt32(List.length nodes)
            nodes |> List.map (writeNodeDesc packet) |>ignore
            
    let read (operationId:uint64) (packet:InPacket) = 
        let opId = enum<NodeOperationIds>(int32(operationId))
        match opId with 
        |NodeOperationIds.NodeIntroductionRequestId -> Some(NodeIntroductionRequest(readNodeDesc packet))
        |NodeOperationIds.NodeIntroductionReplyId ->
            let master = readNodeDesc(packet)
            let count = packet.ReadInt32()
            let nodes = [for _ in 0..count -> readNodeDesc(packet) ]
            Some(NodeIntroductionReply(master, nodes))
        |_ ->None


    

