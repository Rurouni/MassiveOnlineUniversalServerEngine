﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization;
using RakNetWrapper;

namespace MOUSE.Core
{
    public enum NodeMessageId: uint
    {
        Empty = 1,
        ConnectionRequest = 2,
        ConnectionReply = 3,
        UpdateClusterinfo = 4,
        Last //used for protocol generation
    }

    public class EmptyMessage : Message
    {
        public EmptyMessage() : base(NodeMessageId.Empty){}

        public EmptyMessage(NativeReader reader)
            : base(NodeMessageId.Empty, reader)
        {
        }
    }

    public class ConnectRequest : Message
    {
        [DataMember]
        public NodeDescription Description;

        public ConnectRequest(NodeDescription senderDescription) : base((uint)NodeMessageId.ConnectionRequest)
        {
            Description = senderDescription;
        }

        public ConnectRequest(NativeReader reader) : base((uint)NodeMessageId.ConnectionRequest, reader)
        {
            Description = new NodeDescription(reader);
        }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }
    }

    public class ConnectReply : Message
    {
        [DataMember]
        public NodeDescription Description;

        public ConnectReply(NodeDescription receiverDescription) : base((uint)NodeMessageId.ConnectionReply)
        {
            Description = receiverDescription;
        }

        public ConnectReply(NativeReader reader) : base((uint)NodeMessageId.ConnectionReply, reader)
        {
            Description = new NodeDescription(reader);
        }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            Description.Serialize(writer);
        }
    }

    public class UpdateClusterInfo : Message
    {
        [DataMember]
        public List<NodeDescription> Descriptions;

        public UpdateClusterInfo(List<NodeDescription> descriptions): base((uint)NodeMessageId.UpdateClusterinfo)
        {
            Descriptions = descriptions;
        }

        public UpdateClusterInfo(NativeReader reader) : base((uint)NodeMessageId.UpdateClusterinfo, reader)
        {
            int count = reader.ReadInt32();
            Descriptions = new List<NodeDescription>(count);
            for (int i = 0; i < count; i++)
                Descriptions.Add(new NodeDescription(reader));
        }

        public override void Serialize(NativeWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Descriptions.Count);
            foreach (var description in Descriptions)
                description.Serialize(writer);
        }
    }

    [DataContract]
    public class NodeDescription
    {
        [DataMember]
        public readonly ulong NodeId;
        [DataMember]
        public readonly NodeType Type;
        [DataMember]
        public readonly string Ip;
        [DataMember]
        public readonly ushort Port;

        private IPEndPoint _endPoint;
        public IPEndPoint EndPoint
        {
            get
            {
                if(_endPoint == null)
                    _endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
                return _endPoint;
            }
        }

        public NodeDescription(ulong nodeId, NodeType type, IPEndPoint endpoint)
        {
            NodeId = nodeId;
            Type = type;
            _endPoint = endpoint;
            Ip = endpoint.Address.ToString();
            Port = (ushort)endpoint.Port;
        }

        public NodeDescription(NativeReader reader)
        {
            NodeId = reader.ReadUInt64();
            Type = (NodeType)reader.ReadByte();
            Ip = reader.ReadASCII();
            Port = reader.ReadUInt16();
        }

        public void Serialize(NativeWriter writer)
        {
            writer.Write(NodeId);
            writer.Write((byte)Type);
            writer.WriteASCII(Ip);
            writer.Write(Port);
        }


        public override string ToString()
        {
            return string.Format("Node<Id:{0}, Type:{1}, Ip:{2}, Port:{3}>", NodeId, Type, Ip, Port);
        }
    }
}