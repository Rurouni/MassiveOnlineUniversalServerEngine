using MOUSE.Core;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Protocol.Generated
{

	[NetContract(AllowExternalConnections = false)]
	public interface IChatLogin
	{
		Task< LoginResult > Login ( String name );
	}

	[NetContract(AllowExternalConnections = false)]
	public interface IChatService
	{
		Task< List<ChatRoomInfo> > GetRooms (  );
		Task< JoinRoomResponse > JoinOrCreateRoom ( String roomName );
		Task< JoinRoomResponse > JoinRoom ( UInt32 roomId );
	}

	[NetContract(AllowExternalConnections = true)]
	public interface IChatRoomService
	{
		Task< List<String> > Join ( Int64 ticket );
		void Leave (  );
		void Say ( String message );
	}

	[NetContract(AllowExternalConnections = false)]
	public interface IChatRoomServiceCallback
	{
		void OnRoomMessage ( UInt32 roomId, String message );
	}

	[NetContract(AllowExternalConnections = false)]
	public interface IChatManager
	{
		Task< ChatUserInfo > GetUser ( String name );
		Task< ChatUserInfo > TryRegisterUser ( String name );
		void UnregisterUser ( UInt32 id );
		Task< List<ChatRoomInfo> > GetRooms (  );
		Task< UInt32 > GetOrCreateRoom ( String roomName );
		Task< Boolean > DeleteRoom ( UInt32 roomId );
	}

	[NetContract(AllowExternalConnections = false)]
	public interface IChatRoom
	{
		Task< List<ChatUserInfo> > GetUsersInside (  );
		Task< Int64 > AwaitUser ( ChatUserInfo user );
		Task< Boolean > RemoveUser ( UInt32 userId );
	}
	[NetProxy(ContractTypeId = 1279047273, ContractType = typeof(IChatLogin))]
	public sealed class IChatLoginProxy : NodeServiceProxy, IChatLogin
	{
		public async Task< LoginResult > Login ( String name )
		{
			//var request = MessageFactory.New< IChatLoginLoginRequest >();
			var request = new IChatLoginLoginRequest();
			request.name=name;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatLoginLoginReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatLoginLoginRequest), ReplyMessage = typeof(IChatLoginLoginReply))]
		public static async Task<Message> Login(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatLoginLoginRequest)input;
			var retVal = await ((IChatLogin)target).Login(msg.name);
			//var retMsg = msgFactory.New<IChatLoginLoginReply>();
			var retMsg = new IChatLoginLoginReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
	}
	[NetProxy(ContractTypeId = 4131147598, ContractType = typeof(IChatService))]
	public sealed class IChatServiceProxy : NodeServiceProxy, IChatService
	{
		public async Task< List<ChatRoomInfo> > GetRooms (  )
		{
			//var request = MessageFactory.New< IChatServiceGetRoomsRequest >();
			var request = new IChatServiceGetRoomsRequest();
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatServiceGetRoomsReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatServiceGetRoomsRequest), ReplyMessage = typeof(IChatServiceGetRoomsReply))]
		public static async Task<Message> GetRooms(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatServiceGetRoomsRequest)input;
			var retVal = await ((IChatService)target).GetRooms();
			//var retMsg = msgFactory.New<IChatServiceGetRoomsReply>();
			var retMsg = new IChatServiceGetRoomsReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public async Task< JoinRoomResponse > JoinOrCreateRoom ( String roomName )
		{
			//var request = MessageFactory.New< IChatServiceJoinOrCreateRoomRequest >();
			var request = new IChatServiceJoinOrCreateRoomRequest();
			request.roomName=roomName;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatServiceJoinOrCreateRoomReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatServiceJoinOrCreateRoomRequest), ReplyMessage = typeof(IChatServiceJoinOrCreateRoomReply))]
		public static async Task<Message> JoinOrCreateRoom(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatServiceJoinOrCreateRoomRequest)input;
			var retVal = await ((IChatService)target).JoinOrCreateRoom(msg.roomName);
			//var retMsg = msgFactory.New<IChatServiceJoinOrCreateRoomReply>();
			var retMsg = new IChatServiceJoinOrCreateRoomReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public async Task< JoinRoomResponse > JoinRoom ( UInt32 roomId )
		{
			//var request = MessageFactory.New< IChatServiceJoinRoomRequest >();
			var request = new IChatServiceJoinRoomRequest();
			request.roomId=roomId;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatServiceJoinRoomReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatServiceJoinRoomRequest), ReplyMessage = typeof(IChatServiceJoinRoomReply))]
		public static async Task<Message> JoinRoom(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatServiceJoinRoomRequest)input;
			var retVal = await ((IChatService)target).JoinRoom(msg.roomId);
			//var retMsg = msgFactory.New<IChatServiceJoinRoomReply>();
			var retMsg = new IChatServiceJoinRoomReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
	}
	[NetProxy(ContractTypeId = 2616972471, ContractType = typeof(IChatRoomService))]
	public sealed class IChatRoomServiceProxy : NodeServiceProxy, IChatRoomService
	{
		public async Task< List<String> > Join ( Int64 ticket )
		{
			//var request = MessageFactory.New< IChatRoomServiceJoinRequest >();
			var request = new IChatRoomServiceJoinRequest();
			request.ticket=ticket;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatRoomServiceJoinReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceJoinRequest), ReplyMessage = typeof(IChatRoomServiceJoinReply))]
		public static async Task<Message> Join(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomServiceJoinRequest)input;
			var retVal = await ((IChatRoomService)target).Join(msg.ticket);
			//var retMsg = msgFactory.New<IChatRoomServiceJoinReply>();
			var retMsg = new IChatRoomServiceJoinReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public void Leave (  )
		{
			//var request = MessageFactory.New< IChatRoomServiceLeaveRequest >();
			var request = new IChatRoomServiceLeaveRequest();
			ExecuteOneWayServiceOperation(request);
			//MessageFactory.Free(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceLeaveRequest), ReplyMessage = null)]
		public static async Task<Message> Leave(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomServiceLeaveRequest)input;
			((IChatRoomService)target).Leave();
			return null;
		}
		public void Say ( String message )
		{
			//var request = MessageFactory.New< IChatRoomServiceSayRequest >();
			var request = new IChatRoomServiceSayRequest();
			request.message=message;
			ExecuteOneWayServiceOperation(request);
			//MessageFactory.Free(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceSayRequest), ReplyMessage = null)]
		public static async Task<Message> Say(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomServiceSayRequest)input;
			((IChatRoomService)target).Say(msg.message);
			return null;
		}
	}
	[NetProxy(ContractTypeId = 3421052361, ContractType = typeof(IChatRoomServiceCallback))]
	public sealed class IChatRoomServiceCallbackProxy : NodeServiceProxy, IChatRoomServiceCallback
	{
		public void OnRoomMessage ( UInt32 roomId, String message )
		{
			//var request = MessageFactory.New< IChatRoomServiceCallbackOnRoomMessageRequest >();
			var request = new IChatRoomServiceCallbackOnRoomMessageRequest();
			request.roomId=roomId;
			request.message=message;
			ExecuteOneWayServiceOperation(request);
			//MessageFactory.Free(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceCallbackOnRoomMessageRequest), ReplyMessage = null)]
		public static async Task<Message> OnRoomMessage(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomServiceCallbackOnRoomMessageRequest)input;
			((IChatRoomServiceCallback)target).OnRoomMessage(msg.roomId, msg.message);
			return null;
		}
	}
	[NetProxy(ContractTypeId = 1635796547, ContractType = typeof(IChatManager))]
	public sealed class IChatManagerProxy : NodeServiceProxy, IChatManager
	{
		public async Task< ChatUserInfo > GetUser ( String name )
		{
			//var request = MessageFactory.New< IChatManagerGetUserRequest >();
			var request = new IChatManagerGetUserRequest();
			request.name=name;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatManagerGetUserReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatManagerGetUserRequest), ReplyMessage = typeof(IChatManagerGetUserReply))]
		public static async Task<Message> GetUser(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatManagerGetUserRequest)input;
			var retVal = await ((IChatManager)target).GetUser(msg.name);
			//var retMsg = msgFactory.New<IChatManagerGetUserReply>();
			var retMsg = new IChatManagerGetUserReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public async Task< ChatUserInfo > TryRegisterUser ( String name )
		{
			//var request = MessageFactory.New< IChatManagerTryRegisterUserRequest >();
			var request = new IChatManagerTryRegisterUserRequest();
			request.name=name;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatManagerTryRegisterUserReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatManagerTryRegisterUserRequest), ReplyMessage = typeof(IChatManagerTryRegisterUserReply))]
		public static async Task<Message> TryRegisterUser(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatManagerTryRegisterUserRequest)input;
			var retVal = await ((IChatManager)target).TryRegisterUser(msg.name);
			//var retMsg = msgFactory.New<IChatManagerTryRegisterUserReply>();
			var retMsg = new IChatManagerTryRegisterUserReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public void UnregisterUser ( UInt32 id )
		{
			//var request = MessageFactory.New< IChatManagerUnregisterUserRequest >();
			var request = new IChatManagerUnregisterUserRequest();
			request.id=id;
			ExecuteOneWayServiceOperation(request);
			//MessageFactory.Free(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatManagerUnregisterUserRequest), ReplyMessage = null)]
		public static async Task<Message> UnregisterUser(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatManagerUnregisterUserRequest)input;
			((IChatManager)target).UnregisterUser(msg.id);
			return null;
		}
		public async Task< List<ChatRoomInfo> > GetRooms (  )
		{
			//var request = MessageFactory.New< IChatManagerGetRoomsRequest >();
			var request = new IChatManagerGetRoomsRequest();
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatManagerGetRoomsReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatManagerGetRoomsRequest), ReplyMessage = typeof(IChatManagerGetRoomsReply))]
		public static async Task<Message> GetRooms(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatManagerGetRoomsRequest)input;
			var retVal = await ((IChatManager)target).GetRooms();
			//var retMsg = msgFactory.New<IChatManagerGetRoomsReply>();
			var retMsg = new IChatManagerGetRoomsReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public async Task< UInt32 > GetOrCreateRoom ( String roomName )
		{
			//var request = MessageFactory.New< IChatManagerGetOrCreateRoomRequest >();
			var request = new IChatManagerGetOrCreateRoomRequest();
			request.roomName=roomName;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatManagerGetOrCreateRoomReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatManagerGetOrCreateRoomRequest), ReplyMessage = typeof(IChatManagerGetOrCreateRoomReply))]
		public static async Task<Message> GetOrCreateRoom(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatManagerGetOrCreateRoomRequest)input;
			var retVal = await ((IChatManager)target).GetOrCreateRoom(msg.roomName);
			//var retMsg = msgFactory.New<IChatManagerGetOrCreateRoomReply>();
			var retMsg = new IChatManagerGetOrCreateRoomReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public async Task< Boolean > DeleteRoom ( UInt32 roomId )
		{
			//var request = MessageFactory.New< IChatManagerDeleteRoomRequest >();
			var request = new IChatManagerDeleteRoomRequest();
			request.roomId=roomId;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatManagerDeleteRoomReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatManagerDeleteRoomRequest), ReplyMessage = typeof(IChatManagerDeleteRoomReply))]
		public static async Task<Message> DeleteRoom(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatManagerDeleteRoomRequest)input;
			var retVal = await ((IChatManager)target).DeleteRoom(msg.roomId);
			//var retMsg = msgFactory.New<IChatManagerDeleteRoomReply>();
			var retMsg = new IChatManagerDeleteRoomReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
	}
	[NetProxy(ContractTypeId = 4011898469, ContractType = typeof(IChatRoom))]
	public sealed class IChatRoomProxy : NodeServiceProxy, IChatRoom
	{
		public async Task< List<ChatUserInfo> > GetUsersInside (  )
		{
			//var request = MessageFactory.New< IChatRoomGetUsersInsideRequest >();
			var request = new IChatRoomGetUsersInsideRequest();
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatRoomGetUsersInsideReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomGetUsersInsideRequest), ReplyMessage = typeof(IChatRoomGetUsersInsideReply))]
		public static async Task<Message> GetUsersInside(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomGetUsersInsideRequest)input;
			var retVal = await ((IChatRoom)target).GetUsersInside();
			//var retMsg = msgFactory.New<IChatRoomGetUsersInsideReply>();
			var retMsg = new IChatRoomGetUsersInsideReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public async Task< Int64 > AwaitUser ( ChatUserInfo user )
		{
			//var request = MessageFactory.New< IChatRoomAwaitUserRequest >();
			var request = new IChatRoomAwaitUserRequest();
			request.user=user;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatRoomAwaitUserReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomAwaitUserRequest), ReplyMessage = typeof(IChatRoomAwaitUserReply))]
		public static async Task<Message> AwaitUser(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomAwaitUserRequest)input;
			var retVal = await ((IChatRoom)target).AwaitUser(msg.user);
			//var retMsg = msgFactory.New<IChatRoomAwaitUserReply>();
			var retMsg = new IChatRoomAwaitUserReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public async Task< Boolean > RemoveUser ( UInt32 userId )
		{
			//var request = MessageFactory.New< IChatRoomRemoveUserRequest >();
			var request = new IChatRoomRemoveUserRequest();
			request.userId=userId;
			Message reply = await ExecuteServiceOperation(request);
			var ret = ((IChatRoomRemoveUserReply)reply).RetVal;
			//MessageFactory.Free(reply);
			//MessageFactory.Free(request);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomRemoveUserRequest), ReplyMessage = typeof(IChatRoomRemoveUserReply))]
		public static async Task<Message> RemoveUser(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomRemoveUserRequest)input;
			var retVal = await ((IChatRoom)target).RemoveUser(msg.userId);
			//var retMsg = msgFactory.New<IChatRoomRemoveUserReply>();
			var retMsg = new IChatRoomRemoveUserReply();
			retMsg.RetVal = retVal;
			return retMsg;
		}
	}
    public sealed class IChatLoginLoginRequest : Message
    {
        public String name;

        public override uint Id
        {
            get { return 2019756658; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(name);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            name = r.ReadString();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatLoginLoginReply : Message
    {
        public LoginResult RetVal;

        public override uint Id
        {
            get { return 1128145376; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write((Byte)RetVal);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = (LoginResult)r.ReadByte();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatServiceGetRoomsRequest : Message
    {

        public override uint Id
        {
            get { return 1938706274; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatServiceGetRoomsReply : Message
    {
        public List<ChatRoomInfo> RetVal;

        public override uint Id
        {
            get { return 1966421887; }
        }

        public override void Serialize(BinaryWriter w)
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

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    RetVal = null;
                else
                {
                    int lenght = r.ReadInt32();
                    var list = new List<ChatRoomInfo>(lenght);
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
    public sealed class IChatServiceJoinOrCreateRoomRequest : Message
    {
        public String roomName;

        public override uint Id
        {
            get { return 956401361; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(roomName);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            roomName = r.ReadString();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatServiceJoinOrCreateRoomReply : Message
    {
        public JoinRoomResponse RetVal;

        public override uint Id
        {
            get { return 1860964580; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            JoinRoomResponseSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = JoinRoomResponseSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatServiceJoinRoomRequest : Message
    {
        public UInt32 roomId;

        public override uint Id
        {
            get { return 4139561538; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(roomId);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            roomId = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatServiceJoinRoomReply : Message
    {
        public JoinRoomResponse RetVal;

        public override uint Id
        {
            get { return 693987992; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            JoinRoomResponseSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = JoinRoomResponseSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomServiceJoinRequest : Message
    {
        public Int64 ticket;

        public override uint Id
        {
            get { return 3112933142; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(ticket);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            ticket = r.ReadInt64();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomServiceJoinReply : Message
    {
        public List<String> RetVal;

        public override uint Id
        {
            get { return 4292680201; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            if(RetVal != null)
            {
                w.Write(true);
                w.Write((int)RetVal.Count);
                foreach(var element in RetVal)
                    w.Write(element);
            }
            else
                w.Write(false);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    RetVal = null;
                else
                {
                    int lenght = r.ReadInt32();
                    var list = new List<String>(lenght);
                    for(int i = 0; i < lenght; i++)
                    {
                        var x = r.ReadString();
                        list.Add(x);
                    }
                    RetVal = list;
                }
            }
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomServiceLeaveRequest : Message
    {

        public override uint Id
        {
            get { return 3592121337; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomServiceSayRequest : Message
    {
        public String message;

        public override uint Id
        {
            get { return 999376688; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(message);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            message = r.ReadString();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomServiceCallbackOnRoomMessageRequest : Message
    {
        public UInt32 roomId;
        public String message;

        public override uint Id
        {
            get { return 673625236; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(roomId);
            w.Write(message);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            roomId = r.ReadUInt32();
            message = r.ReadString();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerGetUserRequest : Message
    {
        public String name;

        public override uint Id
        {
            get { return 599658246; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(name);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            name = r.ReadString();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerGetUserReply : Message
    {
        public ChatUserInfo RetVal;

        public override uint Id
        {
            get { return 855131526; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            ChatUserInfoSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = ChatUserInfoSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerTryRegisterUserRequest : Message
    {
        public String name;

        public override uint Id
        {
            get { return 78289361; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(name);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            name = r.ReadString();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerTryRegisterUserReply : Message
    {
        public ChatUserInfo RetVal;

        public override uint Id
        {
            get { return 2842931784; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            ChatUserInfoSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = ChatUserInfoSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerUnregisterUserRequest : Message
    {
        public UInt32 id;

        public override uint Id
        {
            get { return 2572877251; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(id);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            id = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerGetRoomsRequest : Message
    {

        public override uint Id
        {
            get { return 2417978606; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerGetRoomsReply : Message
    {
        public List<ChatRoomInfo> RetVal;

        public override uint Id
        {
            get { return 2603755715; }
        }

        public override void Serialize(BinaryWriter w)
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

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    RetVal = null;
                else
                {
                    int lenght = r.ReadInt32();
                    var list = new List<ChatRoomInfo>(lenght);
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
    public sealed class IChatManagerGetOrCreateRoomRequest : Message
    {
        public String roomName;

        public override uint Id
        {
            get { return 1850635672; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(roomName);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            roomName = r.ReadString();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerGetOrCreateRoomReply : Message
    {
        public UInt32 RetVal;

        public override uint Id
        {
            get { return 3183698915; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(RetVal);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerDeleteRoomRequest : Message
    {
        public UInt32 roomId;

        public override uint Id
        {
            get { return 3478560237; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(roomId);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            roomId = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatManagerDeleteRoomReply : Message
    {
        public Boolean RetVal;

        public override uint Id
        {
            get { return 85503512; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(RetVal);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = r.ReadBoolean();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomGetUsersInsideRequest : Message
    {

        public override uint Id
        {
            get { return 1291772870; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomGetUsersInsideReply : Message
    {
        public List<ChatUserInfo> RetVal;

        public override uint Id
        {
            get { return 610270170; }
        }

        public override void Serialize(BinaryWriter w)
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

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    RetVal = null;
                else
                {
                    int lenght = r.ReadInt32();
                    var list = new List<ChatUserInfo>(lenght);
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
    public sealed class IChatRoomAwaitUserRequest : Message
    {
        public ChatUserInfo user;

        public override uint Id
        {
            get { return 2922379568; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            ChatUserInfoSerializer.Serialize(user, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            user = ChatUserInfoSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomAwaitUserReply : Message
    {
        public Int64 RetVal;

        public override uint Id
        {
            get { return 4092941113; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(RetVal);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = r.ReadInt64();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomRemoveUserRequest : Message
    {
        public UInt32 userId;

        public override uint Id
        {
            get { return 3371572576; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(userId);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            userId = r.ReadUInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class IChatRoomRemoveUserReply : Message
    {
        public Boolean RetVal;

        public override uint Id
        {
            get { return 519030747; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(RetVal);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            RetVal = r.ReadBoolean();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
	
	public enum LoginResult : byte
	{
		Ok = 0,
		NameInUse = 1,
		AlreadyRegistered = 2,
	}
	
	public enum JoinRoomInvalidRetCode : int
	{
		RoomNotFound = 0,
		ClientNotAwaited = 1,
	}
	
	[DataContract]
	public class JoinRoomResponse
	{
		[DataMember]
		public UInt32 RoomId;
		[DataMember]
		public Int64 Ticket;
		[DataMember]
		public String ServerEndpoint;
	}
	
	[DataContract]
	public class ChatRoomInfo
	{
		[DataMember]
		public UInt32 Id;
		[DataMember]
		public String Name;
	}
	
	[DataContract]
	public class ChatUserInfo
	{
		[DataMember]
		public UInt32 Id;
		[DataMember]
		public String Name;
	}
	
    public static class JoinRoomResponseSerializer	
    {
        public static void Serialize(JoinRoomResponse x, BinaryWriter w)
        {
            if(x == null)
            {
                w.Write(false);
                return;
            }
            w.Write(true);
            w.Write(x.RoomId);
            w.Write(x.Ticket);
            w.Write(x.ServerEndpoint);
        }
        
        public static JoinRoomResponse Deserialize(BinaryReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new JoinRoomResponse();
            ret.RoomId = r.ReadUInt32();
            ret.Ticket = r.ReadInt64();
            ret.ServerEndpoint = r.ReadString();
            return ret;
        }
    }
	
    public static class ChatRoomInfoSerializer	
    {
        public static void Serialize(ChatRoomInfo x, BinaryWriter w)
        {
            if(x == null)
            {
                w.Write(false);
                return;
            }
            w.Write(true);
            w.Write(x.Id);
            w.Write(x.Name);
        }
        
        public static ChatRoomInfo Deserialize(BinaryReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new ChatRoomInfo();
            ret.Id = r.ReadUInt32();
            ret.Name = r.ReadString();
            return ret;
        }
    }
	
    public static class ChatUserInfoSerializer	
    {
        public static void Serialize(ChatUserInfo x, BinaryWriter w)
        {
            if(x == null)
            {
                w.Write(false);
                return;
            }
            w.Write(true);
            w.Write(x.Id);
            w.Write(x.Name);
        }
        
        public static ChatUserInfo Deserialize(BinaryReader r)
        {
            {
                bool isNotNull = r.ReadBoolean();
                if(!isNotNull)
                    return null;
            }
            var ret = new ChatUserInfo();
            ret.Id = r.ReadUInt32();
            ret.Name = r.ReadString();
            return ret;
        }
    }
}


