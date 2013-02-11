using MOUSE.Core;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;

#pragma warning disable 1998

namespace Protocol.Generated
{

	[NetContract(AllowExternalConnections = false, IsPrimary = true)]
	public interface IChatLogin
	{
		Task< LoginResult > Login ( String name );
	}

	[NetContract(AllowExternalConnections = false, IsPrimary = true)]
	public interface IChatService
	{
		Task< List<String> > GetRooms (  );
		Task< JoinRoomResponse > JoinOrCreateRoom ( String roomName );
	}

	[NetContract(AllowExternalConnections = true, IsPrimary = false)]
	public interface IChatRoomService
	{
		Task< List<String> > Join ( Int64 ticket );
		void Leave (  );
		void Say ( String message );
	}

	[NetContract(AllowExternalConnections = false, IsPrimary = true)]
	public interface IChatRoomServiceCallback
	{
		void OnRoomMessage ( String roomName, String message );
	}
	[NetProxy(ContractTypeId = 1279047273, ContractType = typeof(IChatLogin))]
	public sealed class IChatLoginProxy : NetProxy, IChatLogin
	{
		public async Task< LoginResult > Login ( String name )
		{
			//var request = MessageFactory.New< IChatLoginLoginRequest >();
			var request = new IChatLoginLoginRequest();
			request.name=name;
			Message reply = await ExecuteOperation(request);
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
	public sealed class IChatServiceProxy : NetProxy, IChatService
	{
		public async Task< List<String> > GetRooms (  )
		{
			//var request = MessageFactory.New< IChatServiceGetRoomsRequest >();
			var request = new IChatServiceGetRoomsRequest();
			Message reply = await ExecuteOperation(request);
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
			Message reply = await ExecuteOperation(request);
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
	}
	[NetProxy(ContractTypeId = 2616972471, ContractType = typeof(IChatRoomService))]
	public sealed class IChatRoomServiceProxy : NetProxy, IChatRoomService
	{
		public async Task< List<String> > Join ( Int64 ticket )
		{
			//var request = MessageFactory.New< IChatRoomServiceJoinRequest >();
			var request = new IChatRoomServiceJoinRequest();
			request.ticket=ticket;
			Message reply = await ExecuteOperation(request);
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
			ExecuteOneWayOperation(request);
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
			ExecuteOneWayOperation(request);
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
	public sealed class IChatRoomServiceCallbackProxy : NetProxy, IChatRoomServiceCallback
	{
		public void OnRoomMessage ( String roomName, String message )
		{
			//var request = MessageFactory.New< IChatRoomServiceCallbackOnRoomMessageRequest >();
			var request = new IChatRoomServiceCallbackOnRoomMessageRequest();
			request.roomName=roomName;
			request.message=message;
			ExecuteOneWayOperation(request);
			//MessageFactory.Free(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceCallbackOnRoomMessageRequest), ReplyMessage = null)]
		public static async Task<Message> OnRoomMessage(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomServiceCallbackOnRoomMessageRequest)input;
			((IChatRoomServiceCallback)target).OnRoomMessage(msg.roomName, msg.message);
			return null;
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
        public List<String> RetVal;

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
        public String roomName;
        public String message;

        public override uint Id
        {
            get { return 673625236; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
            w.Write(roomName);
            w.Write(message);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
            roomName = r.ReadString();
            message = r.ReadString();
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
	
	[DataContract]
	public class JoinRoomResponse
	{
		[DataMember]
		public UInt32 RoomActorId;
		[DataMember]
		public Int64 Ticket;
		[DataMember]
		public String ServerEndpoint;
	}
	
	public enum JoinRoomInvalidRetCode : int
	{
		RoomNotFound = 0,
		ClientNotAwaited = 1,
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
            w.Write(x.RoomActorId);
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
            ret.RoomActorId = r.ReadUInt32();
            ret.Ticket = r.ReadInt64();
            ret.ServerEndpoint = r.ReadString();
            return ret;
        }
    }
}


