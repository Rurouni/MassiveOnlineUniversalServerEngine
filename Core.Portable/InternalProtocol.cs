using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Globalization;
using MOUSE.Core.Actors;

namespace MOUSE.Core
{
    public enum MessageId : uint
    {
        Empty = 1,
        ConnectionRequest = 2,
        ConnectionReply = 3,
        InvalidOperation = 4,
        SetInitialActorsList = 5,
        Last, //used for protocol generation
    }

    [DataContract]
    public sealed class EmptyMessage : Message
    {
        public override uint Id { get { return (uint)MessageId.Empty; } }
    }

    [DataContract]
    public sealed class ConnectRequest : Message
    {
        [DataMember]
        public NodeRemoteInfo Info;

        public override uint Id { get { return (uint)MessageId.ConnectionRequest; } }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            NodeRemoteInfoSerializer.Serialize(Info, writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Info = NodeRemoteInfoSerializer.Deserialize(reader);
        }
    }

    [DataContract]
    public sealed class ConnectReply : Message
    {
        [DataMember]
        public NodeRemoteInfo Info;

        public override uint Id { get { return (uint)MessageId.ConnectionReply; } }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            NodeRemoteInfoSerializer.Serialize(Info, writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Info = NodeRemoteInfoSerializer.Deserialize(reader);
        }
    }

    [DataContract]
    public sealed class InvalidOperation : Message
    {
        [DataMember]
        public ushort ErrorCode;
        [DataMember]
        public string DebugDescription;

        public override uint Id { get { return (uint)MessageId.InvalidOperation; } }

        public InvalidOperation()
        {
            ErrorCode = 0;
            DebugDescription = null;
        }

        public InvalidOperation(ushort errorCode, string debugDescription)
        {
            ErrorCode = errorCode;
            DebugDescription = debugDescription;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ErrorCode);
            writer.Write(DebugDescription);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ErrorCode = reader.ReadUInt16();
            DebugDescription = reader.ReadString();
        }
    }

    [DataContract]
    public sealed class SetInitialActorsList : Message
    {
        [DataMember]
        public List<ActorRemoteInfo> Actors;

        public SetInitialActorsList()
        {}

        public SetInitialActorsList(List<ActorRemoteInfo> actors)
        {
            Actors = actors;
        }

        public override uint Id
        {
            get { return (uint)MessageId.SetInitialActorsList; }
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Actors.Count);
            foreach (ActorRemoteInfo actor in Actors)
            {
                ActorRemoteInfoSerializer.Serialize(actor, writer);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            int count = reader.ReadInt32();
            Actors = new List<ActorRemoteInfo>(count);
            for (int i = 0; i < count; i++)
            {
                Actors.Add(ActorRemoteInfoSerializer.Deserialize(reader));
            }
        }
    }

    [DataContract]
    public class NodeRemoteInfo
    {
        [DataMember]
        public readonly ulong NodeId;
        [DataMember]
        public readonly string InternalAddress;
        [DataMember]
        public readonly string ExternalAddress;

        public uint? LocalConnectionId { get; set; }

        public NodeRemoteInfo(ulong nodeId, IPEndPoint internalEndpoint, IPEndPoint externalEndpoint)
        {
            NodeId = nodeId;
            InternalAddress = internalEndpoint.ToString();
            ExternalAddress = externalEndpoint.ToString();
        }

        public NodeRemoteInfo(ulong nodeId, string internalAddress, string externalAddress)
        {
            NodeId = nodeId;
            InternalAddress = internalAddress;
            ExternalAddress = externalAddress;
        }

        public IPEndPoint InternalEndpoint
        {
            get { return ParseIPEndPoint(InternalAddress); }
        }

        public IPEndPoint ExternalEndpoint
        {
            get { return ParseIPEndPoint(ExternalAddress); }
        }

        public static IPEndPoint ParseIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }


        public override string ToString()
        {
            return string.Format("Node<Id:{0}, InternalAddress:{1}, ExternalAddress:{2}>", NodeId, InternalAddress, ExternalAddress);
        }
    }


    public static class NodeRemoteInfoSerializer
    {
        public static void Serialize(NodeRemoteInfo obj, BinaryWriter writer)
        {
            writer.Write(obj.NodeId);
            writer.Write(obj.InternalAddress);
            writer.Write(obj.ExternalAddress);
        }

        public static NodeRemoteInfo Deserialize(BinaryReader reader)
        {
            return new NodeRemoteInfo(
                reader.ReadUInt64(),
                reader.ReadString(),
                reader.ReadString());
        }
    }

    public static class StringSerializer
    {
        public static void Serialize(string str, BinaryWriter writer)
        {
            writer.Write(str);
        }

        public static string Deserialize(BinaryReader reader)
        {
            return reader.ReadString();
        }
    }

}
