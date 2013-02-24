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
	public interface ISomeService
	{
		Task< Int32 > Simple ( Int32 requestId );
		void SimpleOneWay (  );
		Task< ComplexData > Complex ( Int32 requestId, ComplexData data, String name, List<SubData> datas );
	}
	[NetProxy(ContractTypeId = 3211691920, ContractType = typeof(ISomeService))]
	public sealed class ISomeServiceProxy : NetProxy, ISomeService
	{
		public async Task< Int32 > Simple ( Int32 requestId )
		{
			var request = MessageFactory.New< ISomeServiceSimpleRequest >();
			request.requestId=requestId;
			Message reply = await ExecuteOperation(request);
			var ret = ((ISomeServiceSimpleReply)reply).RetVal;
			MessageFactory.Free(reply);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(ISomeServiceSimpleRequest), ReplyMessage = typeof(ISomeServiceSimpleReply))]
		public static async Task<Message> Simple(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (ISomeServiceSimpleRequest)input;
			var retVal = await ((ISomeService)target).Simple(msg.requestId);
			var retMsg = msgFactory.New<ISomeServiceSimpleReply>();
			retMsg.RetVal = retVal;
			return retMsg;
		}
		public void SimpleOneWay (  )
		{
			var request = MessageFactory.New< ISomeServiceSimpleOneWayRequest >();
			ExecuteOneWayOperation(request);
		}
		[NetOperationDispatcher(RequestMessage = typeof(ISomeServiceSimpleOneWayRequest), ReplyMessage = null)]
		public static async Task<Message> SimpleOneWay(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (ISomeServiceSimpleOneWayRequest)input;
			((ISomeService)target).SimpleOneWay();
			return null;
		}
		public async Task< ComplexData > Complex ( Int32 requestId, ComplexData data, String name, List<SubData> datas )
		{
			var request = MessageFactory.New< ISomeServiceComplexRequest >();
			request.requestId=requestId;
			request.data=data;
			request.name=name;
			request.datas=datas;
			Message reply = await ExecuteOperation(request);
			var ret = ((ISomeServiceComplexReply)reply).RetVal;
			MessageFactory.Free(reply);
			return ret;
		}
		[NetOperationDispatcher(RequestMessage = typeof(ISomeServiceComplexRequest), ReplyMessage = typeof(ISomeServiceComplexReply))]
		public static async Task<Message> Complex(IMessageFactory msgFactory, object target, Message input)
		{
			var msg = (ISomeServiceComplexRequest)input;
			var retVal = await ((ISomeService)target).Complex(msg.requestId, msg.data, msg.name, msg.datas);
			var retMsg = msgFactory.New<ISomeServiceComplexReply>();
			retMsg.RetVal = retVal;
			return retMsg;
		}
	}
    public sealed class ISomeServiceSimpleRequest : Message
    {
        public Int32 requestId;

        public override uint Id
        {
            get { return 774712997; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
			w.Write(requestId);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			requestId = r.ReadInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class ISomeServiceSimpleReply : Message
    {
        public Int32 RetVal;

        public override uint Id
        {
            get { return 836643916; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
			w.Write(RetVal);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			RetVal = r.ReadInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class ISomeServiceSimpleOneWayRequest : Message
    {

        public override uint Id
        {
            get { return 3220608553; }
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
        public override MessageReliability Reliability { get { return MessageReliability.Unreliable; } }
    }
    public sealed class ISomeServiceComplexRequest : Message
    {
        public Int32 requestId;
        public ComplexData data;
        public String name;
        public List<SubData> datas;

        public override uint Id
        {
            get { return 1440539548; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
			w.Write(requestId);
			ComplexDataSerializer.Serialize(data, w);
			StringSerializer.Serialize(name, w);
			if(datas != null)
			{
				w.Write(true);
				w.Write((int)datas.Count);
				foreach(var element in datas)
					SubDataSerializer.Serialize(element, w);
			}
			else
				w.Write(false);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			requestId = r.ReadInt32();
			data = ComplexDataSerializer.Deserialize(r);
			name = StringSerializer.Deserialize(r);
			{
				if(!r.ReadBoolean())
					datas = null;
				else
				{
					int lenght = r.ReadInt32();
					var list = new List<SubData>(lenght);
					for(int i = 0; i < lenght; i++)
					{
						var x = SubDataSerializer.Deserialize(r);
						list.Add(x);
					}
					datas = list;
				}
			}
        }

        public override MessagePriority Priority { get { return MessagePriority.High; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    public sealed class ISomeServiceComplexReply : Message
    {
        public ComplexData RetVal;

        public override uint Id
        {
            get { return 771824312; }
        }

        public override void Serialize(BinaryWriter w)
        {
            base.Serialize(w);
			ComplexDataSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(BinaryReader r)
        {
            base.Deserialize(r);
			RetVal = ComplexDataSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.High; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
	
	[DataContract]
	public class SubData
	{
		public Int32 SomeInt1;
		public Int32 SomeInt2;
	}
	
	[DataContract]
	public class ComplexData
	{
		public Byte SomeByte;
		public Int16 SomeShort;
		public UInt16 SomeUShort;
		public Int32 SomeInt;
		public UInt32 SomeUInt;
		public Int64 SomeLong;
		public UInt64 SomeULong;
		public String SomeString;
		public List<String> SomeArrString;
		public List<SubData> SomeArrRec;
	}
	
	public static class SubDataSerializer	
	{
		public static void Serialize(SubData x, BinaryWriter w)
		{
			if(x == null)
			{
				w.Write(false);
				return;
			}
			w.Write(true);
			w.Write(x.SomeInt1);
			w.Write(x.SomeInt2);
		}
		
		public static SubData Deserialize(BinaryReader r)
		{
			{
				bool isNotNull = r.ReadBoolean();
				if(!isNotNull)
					return null;
			}
			var ret = new SubData();
			ret.SomeInt1 = r.ReadInt32();
			ret.SomeInt2 = r.ReadInt32();
			return ret;
		}
	}
	
	public static class ComplexDataSerializer	
	{
		public static void Serialize(ComplexData x, BinaryWriter w)
		{
			if(x == null)
			{
				w.Write(false);
				return;
			}
			w.Write(true);
			w.Write(x.SomeByte);
			w.Write(x.SomeShort);
			w.Write(x.SomeUShort);
			w.Write(x.SomeInt);
			w.Write(x.SomeUInt);
			w.Write(x.SomeLong);
			w.Write(x.SomeULong);
			StringSerializer.Serialize(x.SomeString, w);
			if(x.SomeArrString != null)
			{
				w.Write(true);
				w.Write((int)x.SomeArrString.Count);
				foreach(var element in x.SomeArrString)
					StringSerializer.Serialize(element, w);
			}
			else
				w.Write(false);
			if(x.SomeArrRec != null)
			{
				w.Write(true);
				w.Write((int)x.SomeArrRec.Count);
				foreach(var element in x.SomeArrRec)
					SubDataSerializer.Serialize(element, w);
			}
			else
				w.Write(false);
		}
		
		public static ComplexData Deserialize(BinaryReader r)
		{
			{
				bool isNotNull = r.ReadBoolean();
				if(!isNotNull)
					return null;
			}
			var ret = new ComplexData();
			ret.SomeByte = r.ReadByte();
			ret.SomeShort = r.ReadInt16();
			ret.SomeUShort = r.ReadUInt16();
			ret.SomeInt = r.ReadInt32();
			ret.SomeUInt = r.ReadUInt32();
			ret.SomeLong = r.ReadInt64();
			ret.SomeULong = r.ReadUInt64();
			ret.SomeString = StringSerializer.Deserialize(r);
			{
				if(!r.ReadBoolean())
					ret.SomeArrString = null;
				else
				{
					int lenght = r.ReadInt32();
					var list = new List<String>(lenght);
					for(int i = 0; i < lenght; i++)
					{
						var x = StringSerializer.Deserialize(r);
						list.Add(x);
					}
					ret.SomeArrString = list;
				}
			}
			{
				if(!r.ReadBoolean())
					ret.SomeArrRec = null;
				else
				{
					int lenght = r.ReadInt32();
					var list = new List<SubData>(lenght);
					for(int i = 0; i < lenght; i++)
					{
						var x = SubDataSerializer.Deserialize(r);
						list.Add(x);
					}
					ret.SomeArrRec = list;
				}
			}
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
			s_messages.Add(new ISomeServiceSimpleRequest());
			s_messages.Add(new ISomeServiceSimpleReply());
			s_messages.Add(new ISomeServiceSimpleOneWayRequest());
			s_messages.Add(new ISomeServiceComplexRequest());
			s_messages.Add(new ISomeServiceComplexReply());
			s_messages.Add(new MOUSE.Core.EmptyMessage());
			s_messages.Add(new MOUSE.Core.ConnectRequest());
			s_messages.Add(new MOUSE.Core.ConnectReply());
			s_messages.Add(new MOUSE.Core.InvalidOperation());
			s_messages.Add(new MOUSE.Core.SetInitialActorsList());

			s_proxies = new List<NetProxy>();
			s_proxies.Add(new ISomeServiceProxy());
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


