using MOUSE.Core;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;

namespace Protocol.Generated
{
    [Export(typeof(NodeEntityProxy))]
    [NodeEntityProxy(EntityTypeId = 1996445736, ContractType = typeof(TestDomain.ITestEntity))]
    public sealed class ITestEntityProxy : NodeEntityProxy, TestDomain.ITestEntity
    {
        public async Task< System.Int32 > Simple ( int requestId )
        {
            var request = Node.MessageFactory.New< ITestEntitySimpleRequest >();
            request.requestId=requestId;
            Message reply = await Node.Execute(request, this);
            var ret = ((ITestEntitySimpleReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NodeEntityOperationDispatcher(RequestMessage = typeof(ITestEntitySimpleRequest), ReplyMessage = typeof(ITestEntitySimpleReply))]
        public static async Task<Message> Simple(INodeEntity entity, Message input)
        {
            var msg = (ITestEntitySimpleRequest)input;
            var retVal = await ((TestDomain.ITestEntity)entity).Simple(msg.requestId);
            var retMsg = entity.Context.Node.MessageFactory.New<ITestEntitySimpleReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
        public void SimpleOneWay (  )
        {
            var request = Node.MessageFactory.New< ITestEntitySimpleOneWayRequest >();
            Node.Execute(request, this);
            Node.MessageFactory.Free(request);
        }
        [NodeEntityOperationDispatcher(RequestMessage = typeof(ITestEntitySimpleOneWayRequest), ReplyMessage = null)]
        public static async Task<Message> SimpleOneWay(INodeEntity entity, Message input)
        {
            var msg = (ITestEntitySimpleOneWayRequest)input;
            ((TestDomain.ITestEntity)entity).SimpleOneWay();
            return null;
        }
        public async Task< TestDomain.ComplexData > Complex ( int requestId, TestDomain.ComplexData data, string name, System.Collections.Generic.List<TestDomain.ComplexData> datas )
        {
            var request = Node.MessageFactory.New< ITestEntityComplexRequest >();
            request.requestId=requestId;
            request.data=data;
            request.name=name;
            request.datas=datas;
            Message reply = await Node.Execute(request, this);
            var ret = ((ITestEntityComplexReply)reply).RetVal;
            Node.MessageFactory.Free(reply);
            Node.MessageFactory.Free(request);
            return ret;
        }
        [NodeEntityOperationDispatcher(RequestMessage = typeof(ITestEntityComplexRequest), ReplyMessage = typeof(ITestEntityComplexReply))]
        public static async Task<Message> Complex(INodeEntity entity, Message input)
        {
            var msg = (ITestEntityComplexRequest)input;
            var retVal = await ((TestDomain.ITestEntity)entity).Complex(msg.requestId, msg.data, msg.name, msg.datas);
            var retMsg = entity.Context.Node.MessageFactory.New<ITestEntityComplexReply>();
            retMsg.RetVal = retVal;
            return retMsg;
        }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class ITestEntitySimpleRequest : Message
    {
        [DataMember]
        public int requestId;

        public override uint Id
        {
            get { return 2653326779; }
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
    [Export(typeof(Message))]
    [DataContract]
    public sealed class ITestEntitySimpleReply : Message
    {
        [DataMember]
        public int RetVal;

        public override uint Id
        {
            get { return 1194432348; }
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
    public sealed class ITestEntitySimpleOneWayRequest : Message
    {

        public override uint Id
        {
            get { return 1942863365; }
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
    [Export(typeof(Message))]
    [DataContract]
    public sealed class ITestEntityComplexRequest : Message
    {
        [DataMember]
        public int requestId;
        [DataMember]
        public TestDomain.ComplexData data;
        [DataMember]
        public string name;
        [DataMember]
        public System.Collections.Generic.List<TestDomain.ComplexData> datas;

        public override uint Id
        {
            get { return 1480430162; }
        }

        public override void Serialize(NativeWriter w)
        {
            base.Serialize(w);
            w.Write(requestId);
    		ComplexDataSerializer.Serialize(data, w);
            w.WriteUnicode(name);
			if(datas != null)
			{
				w.Write(true);
				w.Write((int)datas.Count);
				foreach(var element in datas)
		    		ComplexDataSerializer.Serialize(element, w);
			}
			else
				w.Write(false);
        }

        public override void Deserialize(NativeReader r)
        {
            base.Deserialize(r);
            requestId = r.ReadInt32();
    		data = ComplexDataSerializer.Deserialize(r);
            name = r.ReadUnicode();
            {
				bool isNotNull = r.ReadBoolean();
				if(!isNotNull)
					datas = null;
				else
				{
					int lenght = r.ReadInt32();
					var list = new List< TestDomain.ComplexData >(lenght);
					for(int i = 0; i < lenght; i++)
					{
			    		var x = ComplexDataSerializer.Deserialize(r);
						list.Add(x);
					}
					datas = list;
				}
            }
        }

        public override MessagePriority Priority { get { return MessagePriority.High; } }
        public override MessageReliability Reliability { get { return MessageReliability.ReliableOrdered; } }
    }
    [Export(typeof(Message))]
    [DataContract]
    public sealed class ITestEntityComplexReply : Message
    {
        [DataMember]
        public TestDomain.ComplexData RetVal;

        public override uint Id
        {
            get { return 3998670548; }
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
	
    public static class ComplexDataSerializer	
    {
        public static void Serialize(TestDomain.ComplexData x, NativeWriter w)
        {
			if(x == null)
			{
				w.Write(false);
				return;
			}
			w.Write(true);
            w.Write(x.SomeInt);
            w.Write(x.SomeULong);
            w.WriteUnicode(x.SomeString);
			if(x.SomeArrString != null)
			{
				w.Write(true);
				w.Write((int)x.SomeArrString.Count);
				foreach(var element in x.SomeArrString)
		            w.WriteUnicode(element);
			}
			else
				w.Write(false);
			if(x.SomeArrRec != null)
			{
				w.Write(true);
				w.Write((int)x.SomeArrRec.Count);
				foreach(var element in x.SomeArrRec)
		    		ComplexDataSerializer.Serialize(element, w);
			}
			else
				w.Write(false);
        }
        
        public static TestDomain.ComplexData Deserialize(NativeReader r)
        {
			{
				bool isNotNull = r.ReadBoolean();
				if(!isNotNull)
					return null;
			}
            var ret = new TestDomain.ComplexData();
            ret.SomeInt = r.ReadInt32();
            ret.SomeULong = r.ReadUInt64();
            ret.SomeString = r.ReadUnicode();
            {
				bool isNotNull = r.ReadBoolean();
				if(!isNotNull)
					ret.SomeArrString = null;
				else
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
            }
            {
				bool isNotNull = r.ReadBoolean();
				if(!isNotNull)
					ret.SomeArrRec = null;
				else
				{
					int lenght = r.ReadInt32();
					var list = new List< TestDomain.ComplexData >(lenght);
					for(int i = 0; i < lenght; i++)
					{
			    		var x = ComplexDataSerializer.Deserialize(r);
						list.Add(x);
					}
					ret.SomeArrRec = list;
				}
            }
            return ret;
        }
    }
}






