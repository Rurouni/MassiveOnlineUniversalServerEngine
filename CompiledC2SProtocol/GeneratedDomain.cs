using MOUSE.Core;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;

namespace Protocol.Generated
{
    [Export(typeof(NodeEntityProxy))]
    [NetProxy(EntityTypeId = 4131147598, ContractType = typeof(SampleC2SProtocol.IChatService))]
    public sealed class IChatServiceProxy : NodeEntityProxy, SampleC2SProtocol.IChatService
    {
        public async Task Login ( string name )
        {
            var request = Node.MessageFactory.New< IChatServiceLoginRequest >();
            request.name=name;
            await Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatServiceLoginRequest), ReplyMessage = typeof(IChatServiceLoginReply))]
        public static async Task<Message> Login(INodeEntity entity, Message input)
        {
            var msg = (IChatServiceLoginRequest)input;
            await ((SampleC2SProtocol.IChatService)entity).Login(msg.name);
            return entity.Context.Node.MessageFactory.New<IChatServiceLoginReply>();
        }
        public async Task< System.Collections.Generic.List<SampleC2SProtocol.ChatRoomInfo> > GetRooms (  )
        {
            var request = Node.MessageFactory.New< IChatServiceGetRoomsRequest >();
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatServiceGetRoomsReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatServiceGetRoomsRequest), ReplyMessage = typeof(IChatServiceGetRoomsReply))]
        public static async Task<Message> GetRooms(INodeEntity entity, Message input)
        {
            var msg = (IChatServiceGetRoomsRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatService)entity).GetRooms();
            var retMsg = entity.Context.Node.MessageFactory.New<IChatServiceGetRoomsReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< SampleC2SProtocol.CreateRoomResponse > CreateRoom ( string roomName )
        {
            var request = Node.MessageFactory.New< IChatServiceCreateRoomRequest >();
            request.roomName=roomName;
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatServiceCreateRoomReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatServiceCreateRoomRequest), ReplyMessage = typeof(IChatServiceCreateRoomReply))]
        public static async Task<Message> CreateRoom(INodeEntity entity, Message input)
        {
            var msg = (IChatServiceCreateRoomRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatService)entity).CreateRoom(msg.roomName);
            var retMsg = entity.Context.Node.MessageFactory.New<IChatServiceCreateRoomReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< long > JoinRoom ( uint roomId )
        {
            var request = Node.MessageFactory.New< IChatServiceJoinRoomRequest >();
            request.roomId=roomId;
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatServiceJoinRoomReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatServiceJoinRoomRequest), ReplyMessage = typeof(IChatServiceJoinRoomReply))]
        public static async Task<Message> JoinRoom(INodeEntity entity, Message input)
        {
            var msg = (IChatServiceJoinRoomRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatService)entity).JoinRoom(msg.roomId);
            var retMsg = entity.Context.Node.MessageFactory.New<IChatServiceJoinRoomReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
    }
    [Export(typeof(NodeEntityProxy))]
    [NetProxy(EntityTypeId = 2616972471, ContractType = typeof(SampleC2SProtocol.IChatRoomService))]
    public sealed class IChatRoomServiceProxy : NodeEntityProxy, SampleC2SProtocol.IChatRoomService
    {
        public async Task Join ( long ticket )
        {
            var request = Node.MessageFactory.New< IChatRoomServiceJoinRequest >();
            request.ticket=ticket;
            await Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceJoinRequest), ReplyMessage = typeof(IChatRoomServiceJoinReply))]
        public static async Task<Message> Join(INodeEntity entity, Message input)
        {
            var msg = (IChatRoomServiceJoinRequest)input;
            await ((SampleC2SProtocol.IChatRoomService)entity).Join(msg.ticket);
            return entity.Context.Node.MessageFactory.New<IChatRoomServiceJoinReply>();
        }
        public void Say ( string message )
        {
            var request = Node.MessageFactory.New< IChatRoomServiceSayRequest >();
            request.message=message;
            Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceSayRequest), ReplyMessage = null)]
        public static async Task<Message> Say(INodeEntity entity, Message input)
        {
            var msg = (IChatRoomServiceSayRequest)input;
            ((SampleC2SProtocol.IChatRoomService)entity).Say(msg.message);
            return null;
        }
    }
    [Export(typeof(NodeEntityProxy))]
    [NetProxy(EntityTypeId = 3421052361, ContractType = typeof(SampleC2SProtocol.IChatRoomServiceCallback))]
    public sealed class IChatRoomServiceCallbackProxy : NodeEntityProxy, SampleC2SProtocol.IChatRoomServiceCallback
    {
        public void OnSay ( uint roomId, string userName, string message )
        {
            var request = Node.MessageFactory.New< IChatRoomServiceCallbackOnSayRequest >();
            request.roomId=roomId;
            request.userName=userName;
            request.message=message;
            Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceCallbackOnSayRequest), ReplyMessage = null)]
        public static async Task<Message> OnSay(INodeEntity entity, Message input)
        {
            var msg = (IChatRoomServiceCallbackOnSayRequest)input;
            ((SampleC2SProtocol.IChatRoomServiceCallback)entity).OnSay(msg.roomId, msg.userName, msg.message);
            return null;
        }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceLoginRequest : Message
    {
        [DataMember]
        public string name;

        public override uint Id
        {
            get { return 265457215; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.WriteUnicode(name);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            name = r.ReadUnicode();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceLoginReply : Message
    {

        public override uint Id
        {
            get { return 2252042610; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceGetRoomsRequest : Message
    {

        public override uint Id
        {
            get { return 1938706274; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceGetRoomsReply : Message
    {
        [DataMember]
        public System.Collections.Generic.List<SampleC2SProtocol.ChatRoomInfo> RetVal;

        public override uint Id
        {
            get { return 1966421887; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            if(RetVal != null)
            {
                w.Write(true);
                w.Write((int)RetVal.Count);
                foreach(var element in RetVal)
                    ChatRoomInfoSerializer.Serialize(element, w);
            }
            else
                w.Write(false);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    RetVal = null;
                else
                {
                    int lenght = r.ReadInt32();
                    var list = new List< SampleC2SProtocol.ChatRoomInfo >(lenght);
                    for(int i = 0; i < lenght; i++)
                    {
                        var x = ChatRoomInfoSerializer.Deserialize(r);
                        list.Add(x);
                    }
                    RetVal = list;
                }
            }
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceCreateRoomRequest : Message
    {
        [DataMember]
        public string roomName;

        public override uint Id
        {
            get { return 646834541; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.WriteUnicode(roomName);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            roomName = r.ReadUnicode();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceCreateRoomReply : Message
    {
        [DataMember]
        public SampleC2SProtocol.CreateRoomResponse RetVal;

        public override uint Id
        {
            get { return 1012128215; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            CreateRoomResponseSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            RetVal = CreateRoomResponseSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceJoinRoomRequest : Message
    {
        [DataMember]
        public uint roomId;

        public override uint Id
        {
            get { return 4139561538; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(roomId);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            roomId = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatServiceJoinRoomReply : Message
    {
        [DataMember]
        public long RetVal;

        public override uint Id
        {
            get { return 693987992; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(RetVal);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            RetVal = r.ReadInt64();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatRoomServiceJoinRequest : Message
    {
        [DataMember]
        public long ticket;

        public override uint Id
        {
            get { return 3112933142; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(ticket);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            ticket = r.ReadInt64();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatRoomServiceJoinReply : Message
    {

        public override uint Id
        {
            get { return 4292680201; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatRoomServiceSayRequest : Message
    {
        [DataMember]
        public string message;

        public override uint Id
        {
            get { return 999376688; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.WriteUnicode(message);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            message = r.ReadUnicode();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatRoomServiceCallbackOnSayRequest : Message
    {
        [DataMember]
        public uint roomId;
        [DataMember]
        public string userName;
        [DataMember]
        public string message;

        public override uint Id
        {
            get { return 1697461645; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(roomId);
            w.WriteUnicode(userName);
            w.WriteUnicode(message);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            roomId = r.ReadUInt32();
            userName = r.ReadUnicode();
            message = r.ReadUnicode();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
	
    public static class CreateRoomResponseSerializer	
    {
        public static void Serialize(SampleC2SProtocol.CreateRoomResponse x, NativeWriter w)
        {
            if(x == null)
            {
                w.Write(false);
                return;
            }
            w.Write(true);
        }
        
        public static SampleC2SProtocol.CreateRoomResponse Deserialize(NativeReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new SampleC2SProtocol.CreateRoomResponse();
            return ret;
        }
    }
	
    public static class ChatRoomInfoSerializer	
    {
        public static void Serialize(SampleC2SProtocol.ChatRoomInfo x, NativeWriter w)
        {
            if(x == null)
            {
                w.Write(false);
                return;
            }
            w.Write(true);
            w.Write(x.RoomId);
            w.WriteUnicode(x.RoomName);
        }
        
        public static SampleC2SProtocol.ChatRoomInfo Deserialize(NativeReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new SampleC2SProtocol.ChatRoomInfo();
            ret.RoomId = r.ReadUInt32();
            ret.RoomName = r.ReadUnicode();
            return ret;
        }
    }
}


