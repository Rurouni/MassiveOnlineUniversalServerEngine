using MOUSE.Core;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;

namespace Protocol.Generated
{
    [Export(typeof(NodeEntityProxy))]
    [NetProxy(EntityTypeId = 1635796547, ContractType = typeof(SampleS2SProtocol.IChatManager))]
    public sealed class IChatManagerProxy : NodeEntityProxy, SampleS2SProtocol.IChatManager
    {
        public async Task< SampleS2SProtocol.ChatUserInfo > GetUser ( string name )
        {
            var request = Node.MessageFactory.New< IChatManagerGetUserRequest >();
            request.name=name;
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatManagerGetUserReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerGetUserRequest), ReplyMessage = typeof(IChatManagerGetUserReply))]
        public static async Task<Message> GetUser(INodeEntity entity, Message input)
        {
            var msg = (IChatManagerGetUserRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatManager)entity).GetUser(msg.name);
            var retMsg = entity.Context.Node.MessageFactory.New<IChatManagerGetUserReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< System.Collections.Generic.List<SampleC2SProtocol.ChatRoomInfo> > GetRooms (  )
        {
            var request = Node.MessageFactory.New< IChatManagerGetRoomsRequest >();
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatManagerGetRoomsReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerGetRoomsRequest), ReplyMessage = typeof(IChatManagerGetRoomsReply))]
        public static async Task<Message> GetRooms(INodeEntity entity, Message input)
        {
            var msg = (IChatManagerGetRoomsRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatManager)entity).GetRooms();
            var retMsg = entity.Context.Node.MessageFactory.New<IChatManagerGetRoomsReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< uint > CreateRoom ( string roomName )
        {
            var request = Node.MessageFactory.New< IChatManagerCreateRoomRequest >();
            request.roomName=roomName;
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatManagerCreateRoomReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerCreateRoomRequest), ReplyMessage = typeof(IChatManagerCreateRoomReply))]
        public static async Task<Message> CreateRoom(INodeEntity entity, Message input)
        {
            var msg = (IChatManagerCreateRoomRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatManager)entity).CreateRoom(msg.roomName);
            var retMsg = entity.Context.Node.MessageFactory.New<IChatManagerCreateRoomReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task DeleteRoom ( uint roomId )
        {
            var request = Node.MessageFactory.New< IChatManagerDeleteRoomRequest >();
            request.roomId=roomId;
            await Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerDeleteRoomRequest), ReplyMessage = typeof(IChatManagerDeleteRoomReply))]
        public static async Task<Message> DeleteRoom(INodeEntity entity, Message input)
        {
            var msg = (IChatManagerDeleteRoomRequest)input;
            await ((SampleS2SProtocol.IChatManager)entity).DeleteRoom(msg.roomId);
            return entity.Context.Node.MessageFactory.New<IChatManagerDeleteRoomReply>();
        }
    }
    [Export(typeof(NodeEntityProxy))]
    [NetProxy(EntityTypeId = 4011898469, ContractType = typeof(SampleS2SProtocol.IChatRoom))]
    public sealed class IChatRoomProxy : NodeEntityProxy, SampleS2SProtocol.IChatRoom
    {
        public async Task< System.Collections.Generic.List<SampleS2SProtocol.ChatUserInfo> > GetUsersInside (  )
        {
            var request = Node.MessageFactory.New< IChatRoomGetUsersInsideRequest >();
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatRoomGetUsersInsideReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomGetUsersInsideRequest), ReplyMessage = typeof(IChatRoomGetUsersInsideReply))]
        public static async Task<Message> GetUsersInside(INodeEntity entity, Message input)
        {
            var msg = (IChatRoomGetUsersInsideRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatRoom)entity).GetUsersInside();
            var retMsg = entity.Context.Node.MessageFactory.New<IChatRoomGetUsersInsideReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< System.Collections.Generic.List<string> > AddUser ( SampleS2SProtocol.ChatUserInfo user )
        {
            var request = Node.MessageFactory.New< IChatRoomAddUserRequest >();
            request.user=user;
            Message reply = await Node.Execute(request, this);
            var ret = ((IChatRoomAddUserReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomAddUserRequest), ReplyMessage = typeof(IChatRoomAddUserReply))]
        public static async Task<Message> AddUser(INodeEntity entity, Message input)
        {
            var msg = (IChatRoomAddUserRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatRoom)entity).AddUser(msg.user);
            var retMsg = entity.Context.Node.MessageFactory.New<IChatRoomAddUserReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task RemoveUser ( uint userId )
        {
            var request = Node.MessageFactory.New< IChatRoomRemoveUserRequest >();
            request.userId=userId;
            await Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomRemoveUserRequest), ReplyMessage = typeof(IChatRoomRemoveUserReply))]
        public static async Task<Message> RemoveUser(INodeEntity entity, Message input)
        {
            var msg = (IChatRoomRemoveUserRequest)input;
            await ((SampleS2SProtocol.IChatRoom)entity).RemoveUser(msg.userId);
            return entity.Context.Node.MessageFactory.New<IChatRoomRemoveUserReply>();
        }
        public void Say ( uint userId, string message )
        {
            var request = Node.MessageFactory.New< IChatRoomSayRequest >();
            request.userId=userId;
            request.message=message;
            Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomSayRequest), ReplyMessage = null)]
        public static async Task<Message> Say(INodeEntity entity, Message input)
        {
            var msg = (IChatRoomSayRequest)input;
            ((SampleS2SProtocol.IChatRoom)entity).Say(msg.userId, msg.message);
            return null;
        }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatManagerGetUserRequest : Message
    {
        [DataMember]
        public string name;

        public override uint Id
        {
            get { return 599658246; }
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
    public sealed class IChatManagerGetUserReply : Message
    {
        [DataMember]
        public SampleS2SProtocol.ChatUserInfo RetVal;

        public override uint Id
        {
            get { return 855131526; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            ChatUserInfoSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            RetVal = ChatUserInfoSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatManagerGetRoomsRequest : Message
    {

        public override uint Id
        {
            get { return 2417978606; }
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
    public sealed class IChatManagerGetRoomsReply : Message
    {
        [DataMember]
        public System.Collections.Generic.List<SampleC2SProtocol.ChatRoomInfo> RetVal;

        public override uint Id
        {
            get { return 2603755715; }
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
    public sealed class IChatManagerCreateRoomRequest : Message
    {
        [DataMember]
        public string roomName;

        public override uint Id
        {
            get { return 4143506012; }
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
    public sealed class IChatManagerCreateRoomReply : Message
    {
        [DataMember]
        public uint RetVal;

        public override uint Id
        {
            get { return 2425235879; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(RetVal);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            RetVal = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatManagerDeleteRoomRequest : Message
    {
        [DataMember]
        public uint roomId;

        public override uint Id
        {
            get { return 3478560237; }
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
    public sealed class IChatManagerDeleteRoomReply : Message
    {

        public override uint Id
        {
            get { return 85503512; }
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
    public sealed class IChatRoomGetUsersInsideRequest : Message
    {

        public override uint Id
        {
            get { return 1291772870; }
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
    public sealed class IChatRoomGetUsersInsideReply : Message
    {
        [DataMember]
        public System.Collections.Generic.List<SampleS2SProtocol.ChatUserInfo> RetVal;

        public override uint Id
        {
            get { return 610270170; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            if(RetVal != null)
            {
                w.Write(true);
                w.Write((int)RetVal.Count);
                foreach(var element in RetVal)
                    ChatUserInfoSerializer.Serialize(element, w);
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
                    var list = new List< SampleS2SProtocol.ChatUserInfo >(lenght);
                    for(int i = 0; i < lenght; i++)
                    {
                        var x = ChatUserInfoSerializer.Deserialize(r);
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
    public sealed class IChatRoomAddUserRequest : Message
    {
        [DataMember]
        public SampleS2SProtocol.ChatUserInfo user;

        public override uint Id
        {
            get { return 559289230; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            ChatUserInfoSerializer.Serialize(user, w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            user = ChatUserInfoSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatRoomAddUserReply : Message
    {
        [DataMember]
        public System.Collections.Generic.List<string> RetVal;

        public override uint Id
        {
            get { return 2731939890; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            if(RetVal != null)
            {
                w.Write(true);
                w.Write((int)RetVal.Count);
                foreach(var element in RetVal)
                    w.WriteUnicode(element);
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
                    var list = new List< System.String >(lenght);
                    for(int i = 0; i < lenght; i++)
                    {
                        var x = r.ReadUnicode();
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
    public sealed class IChatRoomRemoveUserRequest : Message
    {
        [DataMember]
        public uint userId;

        public override uint Id
        {
            get { return 3371572576; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(userId);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            userId = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatRoomRemoveUserReply : Message
    {

        public override uint Id
        {
            get { return 519030747; }
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
    public sealed class IChatRoomSayRequest : Message
    {
        [DataMember]
        public uint userId;
        [DataMember]
        public string message;

        public override uint Id
        {
            get { return 3747901728; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(userId);
            w.WriteUnicode(message);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            userId = r.ReadUInt32();
            message = r.ReadUnicode();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
	
    public static class ChatUserInfoSerializer	
    {
        public static void Serialize(SampleS2SProtocol.ChatUserInfo x, NativeWriter w)
        {
            if(x == null)
            {
                w.Write(false);
                return;
            }
            w.Write(true);
        }
        
        public static SampleS2SProtocol.ChatUserInfo Deserialize(NativeReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new SampleS2SProtocol.ChatUserInfo();
            return ret;
        }
    }
}


