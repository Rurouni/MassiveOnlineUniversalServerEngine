using MOUSE.Core;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Protocol.Generated
{
	
	[NodeEntityProxy(EntityTypeId = 967679106, ContractType = typeof(SampleProtocol.ISampleEntity))]
    public sealed class ISampleEntityProxy : NodeEntityProxy, SampleProtocol.ISampleEntity
    {
		public async Task< System.Int32 > Ping ( int requestId )
		{
			var request = Node.MessageFactory.New< ISampleEntityPingRequest >();
			request.requestId=requestId;
			Message reply = await Node.ExecuteEntityOperationAsync(request, this);
			var ret = ((ISampleEntityPingReply)reply).RetVal;
			Node.MessageFactory.Free(reply);
			Node.MessageFactory.Free(request);
			return ret;
		}
		[NodeEntityOperationDispatcher(RequestMessage = typeof(ISampleEntityPingRequest), ReplyMessage = typeof(ISampleEntityPingReply))]
        public static async Task<Message> DispatchPing(NodeEntity entity, Message input)
        {
			var msg = (ISampleEntityPingRequest)input;
			var retVal = await ((SampleProtocol.ISampleEntity)entity).Ping(msg.requestId);
            var retMsg = entity.Node.MessageFactory.New<ISampleEntityPingReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
		public async Task< SampleProtocol.ComplexData > HeavyFunc ( int requestId, SampleProtocol.ComplexData data, string name, System.Collections.Generic.List<SampleProtocol.ComplexData> datas )
		{
			var request = Node.MessageFactory.New< ISampleEntityHeavyFuncRequest >();
			request.requestId=requestId;
			request.data=data;
			request.name=name;
			request.datas=datas;
			Message reply = await Node.ExecuteEntityOperationAsync(request, this);
			var ret = ((ISampleEntityHeavyFuncReply)reply).RetVal;
			Node.MessageFactory.Free(reply);
			Node.MessageFactory.Free(request);
			return ret;
		}
		[NodeEntityOperationDispatcher(RequestMessage = typeof(ISampleEntityHeavyFuncRequest), ReplyMessage = typeof(ISampleEntityHeavyFuncReply))]
        public static async Task<Message> DispatchHeavyFunc(NodeEntity entity, Message input)
        {
			var msg = (ISampleEntityHeavyFuncRequest)input;
			var retVal = await ((SampleProtocol.ISampleEntity)entity).HeavyFunc(msg.requestId, msg.data, msg.name, msg.datas);
            var retMsg = entity.Node.MessageFactory.New<ISampleEntityHeavyFuncReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
		public async void SimpleOneWay (  )
		{
			var request = Node.MessageFactory.New< ISampleEntitySimpleOneWayRequest >();
			Node.ExecuteEntityOperationAsync(request, this);
			Node.MessageFactory.Free(request);
		}
		[NodeEntityOperationDispatcher(RequestMessage = typeof(ISampleEntitySimpleOneWayRequest), ReplyMessage = null)]
        public static async Task<Message> DispatchSimpleOneWay(NodeEntity entity, Message input)
        {
			var msg = (ISampleEntitySimpleOneWayRequest)input;
			((SampleProtocol.ISampleEntity)entity).SimpleOneWay();
			return null;
        }
	}
	public sealed class ISampleEntityPingRequest : Message
    {
		public int requestId;

        public override uint Id
        {
            get { return 3274720995; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(requestId);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            requestId = r.ReadInt32();
        }

        public override MessagePriority Priority { get { return MessagePriority.Medium; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
	public sealed class ISampleEntityPingReply : Message
    {
		public int RetVal;

        public override uint Id
        {
            get { return 3391865991; }
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
	public sealed class ISampleEntityHeavyFuncRequest : Message
    {
		public int requestId;
		public SampleProtocol.ComplexData data;
		public string name;
		public System.Collections.Generic.List<SampleProtocol.ComplexData> datas;

        public override uint Id
        {
            get { return 2088156602; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(requestId);
    		ComplexDataSerializer.Serialize(data, w);
            w.WriteUnicode(name);
            w.Write((int)datas.Count);
            foreach(var element in datas)
        		ComplexDataSerializer.Serialize(element, w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            requestId = r.ReadInt32();
    		data = ComplexDataSerializer.Deserialize(r);
            name = r.ReadUnicode();
            {
                int lenght = r.ReadInt32();
                var list = new List< SampleProtocol.ComplexData >(lenght);
                for(int i = 0; i < lenght; i++)
                {
            		var x = ComplexDataSerializer.Deserialize(r);
                    list.Add(x);
                }
                datas = list;
            }
        }

        public override MessagePriority Priority { get { return MessagePriority.High; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
	public sealed class ISampleEntityHeavyFuncReply : Message
    {
		public SampleProtocol.ComplexData RetVal;

        public override uint Id
        {
            get { return 310634519; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
    		ComplexDataSerializer.Serialize(RetVal, w);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
    		RetVal = ComplexDataSerializer.Deserialize(r);
        }

        public override MessagePriority Priority { get { return MessagePriority.High; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
	public sealed class ISampleEntitySimpleOneWayRequest : Message
    {

        public override uint Id
        {
            get { return 3414967792; }
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
        public override MessageReliability Reliability { get { return MessageReliability.Unreliable; } }
    }
	
    public static class ComplexDataSerializer	
    {
        public static void Serialize(SampleProtocol.ComplexData x, NativeWriter w)
        {
            w.Write(x.SomeInt);
            w.Write(x.SomeULong);
            w.WriteUnicode(x.SomeString);
            w.Write((int)x.SomeArrString.Count);
            foreach(var element in x.SomeArrString)
                w.WriteUnicode(element);
            w.Write((int)x.SomeArrRec.Count);
            foreach(var element in x.SomeArrRec)
        		ComplexDataSerializer.Serialize(element, w);
        }
        
        public static SampleProtocol.ComplexData Deserialize(NativeReader r)
        {
            var ret = new SampleProtocol.ComplexData();
            ret.SomeInt = r.ReadInt32();
            ret.SomeULong = r.ReadUInt64();
            ret.SomeString = r.ReadUnicode();
            {
                int lenght = r.ReadInt32();
                var list = new List< System.String >(lenght);
                for(int i = 0; i < lenght; i++)
                {
                    var x = r.ReadUnicode();
                    list.Add(x);
                }
                ret.SomeArrString = list;
            }
            {
                int lenght = r.ReadInt32();
                var list = new List< SampleProtocol.ComplexData >(lenght);
                for(int i = 0; i < lenght; i++)
                {
            		var x = ComplexDataSerializer.Deserialize(r);
                    list.Add(x);
                }
                ret.SomeArrRec = list;
            }
            return ret;
        }
    }
}




