using MOUSE.Core;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;

namespace Protocol.Generated
{
    [Export(typeof(NodeServiceProxy))]
    [NetProxy(ContractTypeId = 1279047273, ContractType = typeof(SampleC2SProtocol.IChatLogin))]
    public sealed class IChatLoginProxy : NodeServiceProxy, SampleC2SProtocol.IChatLogin
    {
        public async Task< SampleC2SProtocol.LoginResult > Login ( string name )
        {
            var request = Node.MessageFactory.New< IChatLoginLoginRequest >();
            request.name=name;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatLoginLoginReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatLoginLoginRequest), ReplyMessage = typeof(IChatLoginLoginReply))]
        public static async Task<Message> Login(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatLoginLoginRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatLogin)target).Login(msg.name);
            var retMsg = msgFactory.New<IChatLoginLoginReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
    }
    [Export(typeof(NodeServiceProxy))]
    [NetProxy(ContractTypeId = 4131147598, ContractType = typeof(SampleC2SProtocol.IChatService))]
    public sealed class IChatServiceProxy : NodeServiceProxy, SampleC2SProtocol.IChatService
    {
        public async Task< System.Collections.Generic.List<SampleC2SProtocol.ChatRoomInfo> > GetRooms (  )
        {
            var request = Node.MessageFactory.New< IChatServiceGetRoomsRequest >();
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatServiceGetRoomsReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatServiceGetRoomsRequest), ReplyMessage = typeof(IChatServiceGetRoomsReply))]
        public static async Task<Message> GetRooms(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatServiceGetRoomsRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatService)target).GetRooms();
            var retMsg = msgFactory.New<IChatServiceGetRoomsReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< SampleC2SProtocol.CreateRoomResponse > CreateRoom ( string roomName )
        {
            var request = Node.MessageFactory.New< IChatServiceCreateRoomRequest >();
            request.roomName=roomName;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatServiceCreateRoomReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatServiceCreateRoomRequest), ReplyMessage = typeof(IChatServiceCreateRoomReply))]
        public static async Task<Message> CreateRoom(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatServiceCreateRoomRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatService)target).CreateRoom(msg.roomName);
            var retMsg = msgFactory.New<IChatServiceCreateRoomReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< long > JoinRoom ( uint roomId )
        {
            var request = Node.MessageFactory.New< IChatServiceJoinRoomRequest >();
            request.roomId=roomId;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatServiceJoinRoomReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatServiceJoinRoomRequest), ReplyMessage = typeof(IChatServiceJoinRoomReply))]
        public static async Task<Message> JoinRoom(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatServiceJoinRoomRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatService)target).JoinRoom(msg.roomId);
            var retMsg = msgFactory.New<IChatServiceJoinRoomReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
    }
    [Export(typeof(NodeServiceProxy))]
    [NetProxy(ContractTypeId = 2616972471, ContractType = typeof(SampleC2SProtocol.IChatRoomService))]
    public sealed class IChatRoomServiceProxy : NodeServiceProxy, SampleC2SProtocol.IChatRoomService
    {
        public async Task< System.Collections.Generic.List<string> > Join ( long ticket )
        {
            var request = Node.MessageFactory.New< IChatRoomServiceJoinRequest >();
            request.ticket=ticket;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatRoomServiceJoinReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceJoinRequest), ReplyMessage = typeof(IChatRoomServiceJoinReply))]
        public static async Task<Message> Join(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatRoomServiceJoinRequest)input;
            var retVal = await ((SampleC2SProtocol.IChatRoomService)target).Join(msg.ticket);
            var retMsg = msgFactory.New<IChatRoomServiceJoinReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public void Say ( string message )
        {
            var request = Node.MessageFactory.New< IChatRoomServiceSayRequest >();
            request.message=message;
            Node.ExecuteServiceOperation(this, request);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceSayRequest), ReplyMessage = null)]
        public static async Task<Message> Say(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatRoomServiceSayRequest)input;
            ((SampleC2SProtocol.IChatRoomService)target).Say(msg.message);
            return null;
        }
    }
    [Export(typeof(NodeServiceProxy))]
    [NetProxy(ContractTypeId = 3421052361, ContractType = typeof(SampleC2SProtocol.IChatRoomServiceCallback))]
    public sealed class IChatRoomServiceCallbackProxy : NodeServiceProxy, SampleC2SProtocol.IChatRoomServiceCallback
    {
        public void OnRoomMessage ( uint roomId, string message )
        {
            var request = Node.MessageFactory.New< IChatRoomServiceCallbackOnRoomMessageRequest >();
            request.roomId=roomId;
            request.message=message;
            Node.ExecuteServiceOperation(this, request);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceCallbackOnRoomMessageRequest), ReplyMessage = null)]
        public static async Task<Message> OnRoomMessage(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatRoomServiceCallbackOnRoomMessageRequest)input;
            ((SampleC2SProtocol.IChatRoomServiceCallback)target).OnRoomMessage(msg.roomId, msg.message);
            return null;
        }
    }
    [Export(typeof(NodeServiceProxy))]
    [NetProxy(ContractTypeId = 1635796547, ContractType = typeof(SampleS2SProtocol.IChatManager))]
    public sealed class IChatManagerProxy : NodeServiceProxy, SampleS2SProtocol.IChatManager
    {
        public async Task< SampleS2SProtocol.ChatUserInfo > GetUser ( string name )
        {
            var request = Node.MessageFactory.New< IChatManagerGetUserRequest >();
            request.name=name;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatManagerGetUserReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerGetUserRequest), ReplyMessage = typeof(IChatManagerGetUserReply))]
        public static async Task<Message> GetUser(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatManagerGetUserRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatManager)target).GetUser(msg.name);
            var retMsg = msgFactory.New<IChatManagerGetUserReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< uint > RegisterUser ( string name )
        {
            var request = Node.MessageFactory.New< IChatManagerRegisterUserRequest >();
            request.name=name;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatManagerRegisterUserReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerRegisterUserRequest), ReplyMessage = typeof(IChatManagerRegisterUserReply))]
        public static async Task<Message> RegisterUser(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatManagerRegisterUserRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatManager)target).RegisterUser(msg.name);
            var retMsg = msgFactory.New<IChatManagerRegisterUserReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public void UnregisterUser ( uint id )
        {
            var request = Node.MessageFactory.New< IChatManagerUnregisterUserRequest >();
            request.id=id;
            Node.ExecuteServiceOperation(this, request);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerUnregisterUserRequest), ReplyMessage = null)]
        public static async Task<Message> UnregisterUser(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatManagerUnregisterUserRequest)input;
            ((SampleS2SProtocol.IChatManager)target).UnregisterUser(msg.id);
            return null;
        }
        public async Task< System.Collections.Generic.List<SampleC2SProtocol.ChatRoomInfo> > GetRooms (  )
        {
            var request = Node.MessageFactory.New< IChatManagerGetRoomsRequest >();
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatManagerGetRoomsReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerGetRoomsRequest), ReplyMessage = typeof(IChatManagerGetRoomsReply))]
        public static async Task<Message> GetRooms(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatManagerGetRoomsRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatManager)target).GetRooms();
            var retMsg = msgFactory.New<IChatManagerGetRoomsReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< uint > CreateRoom ( string roomName )
        {
            var request = Node.MessageFactory.New< IChatManagerCreateRoomRequest >();
            request.roomName=roomName;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatManagerCreateRoomReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerCreateRoomRequest), ReplyMessage = typeof(IChatManagerCreateRoomReply))]
        public static async Task<Message> CreateRoom(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatManagerCreateRoomRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatManager)target).CreateRoom(msg.roomName);
            var retMsg = msgFactory.New<IChatManagerCreateRoomReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task DeleteRoom ( uint roomId )
        {
            var request = Node.MessageFactory.New< IChatManagerDeleteRoomRequest >();
            request.roomId=roomId;
            await Node.ExecuteServiceOperation(this, request);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatManagerDeleteRoomRequest), ReplyMessage = typeof(IChatManagerDeleteRoomReply))]
        public static async Task<Message> DeleteRoom(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatManagerDeleteRoomRequest)input;
            await ((SampleS2SProtocol.IChatManager)target).DeleteRoom(msg.roomId);
            return msgFactory.New<IChatManagerDeleteRoomReply>();
        }
    }
    [Export(typeof(NodeServiceProxy))]
    [NetProxy(ContractTypeId = 4011898469, ContractType = typeof(SampleS2SProtocol.IChatRoom))]
    public sealed class IChatRoomProxy : NodeServiceProxy, SampleS2SProtocol.IChatRoom
    {
        public async Task< System.Collections.Generic.List<SampleS2SProtocol.ChatUserInfo> > GetUsersInside (  )
        {
            var request = Node.MessageFactory.New< IChatRoomGetUsersInsideRequest >();
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatRoomGetUsersInsideReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomGetUsersInsideRequest), ReplyMessage = typeof(IChatRoomGetUsersInsideReply))]
        public static async Task<Message> GetUsersInside(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatRoomGetUsersInsideRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatRoom)target).GetUsersInside();
            var retMsg = msgFactory.New<IChatRoomGetUsersInsideReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task< System.Collections.Generic.List<string> > AddUser ( SampleS2SProtocol.ChatUserInfo user )
        {
            var request = Node.MessageFactory.New< IChatRoomAddUserRequest >();
            request.user=user;
            Message reply = await Node.ExecuteServiceOperation(this, request);
            var ret = ((IChatRoomAddUserReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomAddUserRequest), ReplyMessage = typeof(IChatRoomAddUserReply))]
        public static async Task<Message> AddUser(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatRoomAddUserRequest)input;
            var retVal = await ((SampleS2SProtocol.IChatRoom)target).AddUser(msg.user);
            var retMsg = msgFactory.New<IChatRoomAddUserReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public async Task RemoveUser ( uint userId )
        {
            var request = Node.MessageFactory.New< IChatRoomRemoveUserRequest >();
            request.userId=userId;
            await Node.ExecuteServiceOperation(this, request);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomRemoveUserRequest), ReplyMessage = typeof(IChatRoomRemoveUserReply))]
        public static async Task<Message> RemoveUser(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatRoomRemoveUserRequest)input;
            await ((SampleS2SProtocol.IChatRoom)target).RemoveUser(msg.userId);
            return msgFactory.New<IChatRoomRemoveUserReply>();
        }
        public void Say ( uint userId, string message )
        {
            var request = Node.MessageFactory.New< IChatRoomSayRequest >();
            request.userId=userId;
            request.message=message;
            Node.ExecuteServiceOperation(this, request);
            Node.MessageFactory.Free(request);
        }
        [NetOperationDispatcher(RequestMessage = typeof(IChatRoomSayRequest), ReplyMessage = null)]
        public static async Task<Message> Say(IMessageFactory msgFactory, object target, Message input)
        {
            var msg = (IChatRoomSayRequest)input;
            ((SampleS2SProtocol.IChatRoom)target).Say(msg.userId, msg.message);
            return null;
        }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class IChatLoginLoginRequest : Message
    {
        [DataMember]
        public string name;

        public override uint Id
        {
            get { return 2019756658; }
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
    public sealed class IChatLoginLoginReply : Message
    {
        [DataMember]
        public SampleC2SProtocol.LoginResult RetVal;

        public override uint Id
        {
            get { return 1128145376; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(RetVal);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            RetVal = r.ReadInt32();
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
        [DataMember]
        public System.Collections.Generic.List<string> RetVal;

        public override uint Id
        {
            get { return 4292680201; }
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
    public sealed class IChatRoomServiceCallbackOnRoomMessageRequest : Message
    {
        [DataMember]
        public uint roomId;
        [DataMember]
        public string message;

        public override uint Id
        {
            get { return 673625236; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(roomId);
            w.WriteUnicode(message);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            roomId = r.ReadUInt32();
            message = r.ReadUnicode();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
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
    public sealed class IChatManagerRegisterUserRequest : Message
    {
        [DataMember]
        public string name;

        public override uint Id
        {
            get { return 35073914; }
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
    public sealed class IChatManagerRegisterUserReply : Message
    {
        [DataMember]
        public uint RetVal;

        public override uint Id
        {
            get { return 125316054; }
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
    public sealed class IChatManagerUnregisterUserRequest : Message
    {
        [DataMember]
        public uint id;

        public override uint Id
        {
            get { return 2572877251; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(id);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            id = r.ReadUInt32();
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
            w.Write(x.Code);
            CreateRoomResponseSubDataSerializer.Serialize(x.Data, w);
        }
        
        public static SampleC2SProtocol.CreateRoomResponse Deserialize(NativeReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new SampleC2SProtocol.CreateRoomResponse();
            ret.Code = r.ReadInt32();
            ret.Data = CreateRoomResponseSubDataSerializer.Deserialize(r);
            return ret;
        }
    }
	
    public static class CreateRoomResponseSubDataSerializer	
    {
        public static void Serialize(SampleC2SProtocol.CreateRoomResponse.CreateRoomResponseSubData x, NativeWriter w)
        {
            if(x == null)
            {
                w.Write(false);
                return;
            }
            w.Write(true);
            w.Write(x.RoomId);
            w.Write(x.Ticket);
        }
        
        public static SampleC2SProtocol.CreateRoomResponse.CreateRoomResponseSubData Deserialize(NativeReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new SampleC2SProtocol.CreateRoomResponse.CreateRoomResponseSubData();
            ret.RoomId = r.ReadUInt32();
            ret.Ticket = r.ReadInt64();
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
            w.Write(x.Id);
            w.Write(x.NodeId);
            w.WriteUnicode(x.Name);
        }
        
        public static SampleS2SProtocol.ChatUserInfo Deserialize(NativeReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new SampleS2SProtocol.ChatUserInfo();
            ret.Id = r.ReadUInt32();
            ret.NodeId = r.ReadInt64();
            ret.Name = r.ReadUnicode();
            return ret;
        }
    }
}


