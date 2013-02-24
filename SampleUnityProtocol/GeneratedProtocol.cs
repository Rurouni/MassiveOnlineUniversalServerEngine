using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MOUSE.Core;
using MOUSE.Unity;

namespace Protocol.Generated
{
	[NetContract]
	public interface IChatLogin
	{
		Future< LoginResult > Login ( String name );
	}
	[NetContract]
	public interface IChatService
	{
		Future< List<String> > GetRooms (  );
		Future< JoinRoomResponse > JoinOrCreateRoom ( String roomName );
	}
	[NetContract]
	public interface IChatRoomService
	{
		Future< List<String> > Join ( Int64 ticket );
		void Leave (  );
		void Say ( String message );
	}
	[NetContract]
	public interface IChatRoomServiceCallback
	{
		void OnRoomMessage ( String roomName, String message );
	}
	[NetProxy(ContractTypeId = 1279047273, ContractType = typeof(IChatLogin))]
	public sealed class IChatLoginProxy : NetProxy, IChatLogin
	{
		public Future< LoginResult > Login ( String name )
		{
			var request = MessageFactory.New< IChatLoginLoginRequest >();
			request.name=name;
			var finalFuture = new Future< LoginResult >();
			ExecuteOperation(request, (replyMsg) => finalFuture.SetResult( ((IChatLoginLoginReply)replyMsg).RetVal), (errorCode, errorDesc) => finalFuture.SetError(errorCode, errorDesc)); 
			return finalFuture;

		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatLoginLoginRequest), ReplyMessage = typeof(IChatLoginLoginReply))]
		public static Message Login(IMessageFactory msgFactory, object target, Message input)
		{
					throw new NotSupportedException();
		}
	}
	[NetProxy(ContractTypeId = 4131147598, ContractType = typeof(IChatService))]
	public sealed class IChatServiceProxy : NetProxy, IChatService
	{
		public Future< List<String> > GetRooms (  )
		{
			var request = MessageFactory.New< IChatServiceGetRoomsRequest >();
			var finalFuture = new Future< List<String> >();
			ExecuteOperation(request, (replyMsg) => finalFuture.SetResult( ((IChatServiceGetRoomsReply)replyMsg).RetVal), (errorCode, errorDesc) => finalFuture.SetError(errorCode, errorDesc)); 
			return finalFuture;

		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatServiceGetRoomsRequest), ReplyMessage = typeof(IChatServiceGetRoomsReply))]
		public static Message GetRooms(IMessageFactory msgFactory, object target, Message input)
		{
					throw new NotSupportedException();
		}
		public Future< JoinRoomResponse > JoinOrCreateRoom ( String roomName )
		{
			var request = MessageFactory.New< IChatServiceJoinOrCreateRoomRequest >();
			request.roomName=roomName;
			var finalFuture = new Future< JoinRoomResponse >();
			ExecuteOperation(request, (replyMsg) => finalFuture.SetResult( ((IChatServiceJoinOrCreateRoomReply)replyMsg).RetVal), (errorCode, errorDesc) => finalFuture.SetError(errorCode, errorDesc)); 
			return finalFuture;

		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatServiceJoinOrCreateRoomRequest), ReplyMessage = typeof(IChatServiceJoinOrCreateRoomReply))]
		public static Message JoinOrCreateRoom(IMessageFactory msgFactory, object target, Message input)
		{
					throw new NotSupportedException();
		}
	}
	[NetProxy(ContractTypeId = 2616972471, ContractType = typeof(IChatRoomService))]
	public sealed class IChatRoomServiceProxy : NetProxy, IChatRoomService
	{
		public Future< List<String> > Join ( Int64 ticket )
		{
			var request = MessageFactory.New< IChatRoomServiceJoinRequest >();
			request.ticket=ticket;
			var finalFuture = new Future< List<String> >();
			ExecuteOperation(request, (replyMsg) => finalFuture.SetResult( ((IChatRoomServiceJoinReply)replyMsg).RetVal), (errorCode, errorDesc) => finalFuture.SetError(errorCode, errorDesc)); 
			return finalFuture;

		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceJoinRequest), ReplyMessage = typeof(IChatRoomServiceJoinReply))]
		public static Message Join(IMessageFactory msgFactory, object target, Message input)
		{
					throw new NotSupportedException();
		}
		public void Leave (  )
		{
			var request = MessageFactory.New< IChatRoomServiceLeaveRequest >();
			ExecuteOneWayOperation(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceLeaveRequest), ReplyMessage = null)]
		public static Message Leave(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (IChatRoomServiceLeaveRequest)input;
			((IChatRoomService)target).Leave();
			return null;
		}
		public void Say ( String message )
		{
			var request = MessageFactory.New< IChatRoomServiceSayRequest >();
			request.message=message;
			ExecuteOneWayOperation(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceSayRequest), ReplyMessage = null)]
		public static Message Say(IMessageFactory msgFactory, object target, Message input)
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
			var request = MessageFactory.New< IChatRoomServiceCallbackOnRoomMessageRequest >();
			request.roomName=roomName;
			request.message=message;
			ExecuteOneWayOperation(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(IChatRoomServiceCallbackOnRoomMessageRequest), ReplyMessage = null)]
		public static Message OnRoomMessage(IMessageFactory msgFactory, object target, Message input)
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
			StringSerializer.Serialize(name, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			name = StringSerializer.Deserialize(r);
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
					StringSerializer.Serialize(element, w);
			}
			else
				w.Write(false);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			{
				if(!r.ReadBoolean())
					RetVal = null;
				else
				{
					int lenght = r.ReadInt32();
					var list = new List<String>(lenght);
					for(int i = 0; i < lenght; i++)
					{
						var x = StringSerializer.Deserialize(r);
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
			StringSerializer.Serialize(roomName, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			roomName = StringSerializer.Deserialize(r);
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
					StringSerializer.Serialize(element, w);
			}
			else
				w.Write(false);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			{
				if(!r.ReadBoolean())
					RetVal = null;
				else
				{
					int lenght = r.ReadInt32();
					var list = new List<String>(lenght);
					for(int i = 0; i < lenght; i++)
					{
						var x = StringSerializer.Deserialize(r);
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
			StringSerializer.Serialize(message, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			message = StringSerializer.Deserialize(r);
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
			StringSerializer.Serialize(roomName, w);
			StringSerializer.Serialize(message, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			roomName = StringSerializer.Deserialize(r);
			message = StringSerializer.Deserialize(r);
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
		public UInt32 RoomActorId;
		public Int64 Ticket;
		public String ServerEndpoint;
	}

	public enum JoinRoomInvalidRetCode : byte
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
			StringSerializer.Serialize(x.ServerEndpoint, w);
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
			ret.ServerEndpoint = StringSerializer.Deserialize(r);
			return ret;
		}
	}

	public static class ProtocolDescription
	{
		private static List<Message> s_messages;
		private static List<NetProxy> s_proxies;

		static ProtocolDescription()
		{
			s_messages  = new List<Message>();
			s_messages.Add(new IChatLoginLoginRequest());
			s_messages.Add(new IChatLoginLoginReply());
			s_messages.Add(new IChatServiceGetRoomsRequest());
			s_messages.Add(new IChatServiceGetRoomsReply());
			s_messages.Add(new IChatServiceJoinOrCreateRoomRequest());
			s_messages.Add(new IChatServiceJoinOrCreateRoomReply());
			s_messages.Add(new IChatRoomServiceJoinRequest());
			s_messages.Add(new IChatRoomServiceJoinReply());
			s_messages.Add(new IChatRoomServiceLeaveRequest());
			s_messages.Add(new IChatRoomServiceSayRequest());
			s_messages.Add(new IChatRoomServiceCallbackOnRoomMessageRequest());
			s_messages.Add(new MOUSE.Core.EmptyMessage());
			s_messages.Add(new MOUSE.Core.ConnectRequest());
			s_messages.Add(new MOUSE.Core.ConnectReply());
			s_messages.Add(new MOUSE.Core.InvalidOperation());
			s_messages.Add(new MOUSE.Core.SetInitialActorsList());

			s_proxies = new List<NetProxy>();
			s_proxies.Add(new IChatLoginProxy());
			s_proxies.Add(new IChatServiceProxy());
			s_proxies.Add(new IChatRoomServiceProxy());
			s_proxies.Add(new IChatRoomServiceCallbackProxy());
		}

		public static List<Message> GetAllMessages()
		{
			return s_messages;
		}

		public static List<NetProxy> GetAllProxies()
		{
			return s_proxies;
		}
	}
}



