using System;
using System.IO;

namespace MOUSE.Core.Interfaces.Serialization
{
    public interface IMessageSerializer
    {
        bool TryDeserialize(ArraySegment<byte> data, out Message msg);
        bool TryReadType(ArraySegment<byte> data, out Type type);
        void Serialize(Message msg, Stream stream);
    }

//    public interface IMessagePool
//    {
//        TMessage New<TMessage>() where TMessage : Message;
//        void Free(Message msg);
//    }

//    //TODO: rework headers so they also could be pooled
//    public class MessageFactory : IMessageFactory
//    {
//        readonly Logger Log = LogManager.GetCurrentClassLogger();
//        private Dictionary<Type, uint> _msgIdByType = new Dictionary<Type,uint>();
//        private Dictionary<uint, Type> _typeByMsgId = new Dictionary<uint,Type>();
//        private readonly ConcurrentDictionary<uint, ConcurrentStack<Message>> _messagePoolByMsgId = new ConcurrentDictionary<uint, ConcurrentStack<Message>>();

        
//        public MessageFactory(IEnumerable<Message> importedMessages)
//        {
//            foreach (var msg in importedMessages)
//            {
//                Type type = msg.GetType();
//                _msgIdByType.Add(type, msg.Id);
//                _typeByMsgId.Add(msg.Id, type);
//                var pool = new ConcurrentStack<Message>();
//                msg.IsPooled = true;
//                pool.Push(msg);
//                _messagePoolByMsgId[msg.Id] = pool;
//                Log.Info("Registered Message<Id:{0} Type:{1}>", msg.Id, type);
//            }
//        }

//        public TMessage New<TMessage>() where TMessage : Message
//        {
//            uint msgId;
//            if(!_msgIdByType.TryGetValue(typeof(TMessage), out msgId))
//                throw new Exception(string.Format("{0} is not registered message", typeof(TMessage).Name));

//           return (TMessage)New(msgId, typeof(TMessage));
//        }

//        private Message New(uint msgId, Type type)
//        {
//            ConcurrentStack<Message> pool = _messagePoolByMsgId[msgId];
//            Message msg;
//            if(pool.TryPop(out msg))
//            {
//                msg.InUse = true;
//#if DEBUG
//                Log.Debug("Got Message<{0}, #{1}> from pool", type.Name, msg.GetHashCode());
//#endif
//                return msg;
//            }
//            else
//            {
//                msg = (Message)FormatterServices.GetUninitializedObject(type);
//                msg.IsPooled = true;
//#if DEBUG
//                Log.Debug("Created Message<{0}, #{1}>", type.Name, msg.GetHashCode());
//#endif
//                msg.InUse = true;
//                return msg;
//            }
//        }

//        public void Free(Message msg)
//        {
//            if (msg.IsPooled)
//            {
//                if (!msg.InUse)
//                {
//                    Log.Warn("Freeing message that is already stored in pool, enable debug build for details");
//                }
//                msg.InUse = false;
//                msg.ClearHeaders();
//                _messagePoolByMsgId[msg.Id].Push(msg);
//#if DEBUG
//                Log.Debug("Returned Message<{0}, #{1}> from pool", msg.GetType().Name, msg.GetHashCode());
//#endif
//            }
//            else
//            {
//                Log.Warn("Freeing message not from pool, enable debug build for details");
//            }
//        }

//        public Message Deserialize(BinaryReader reader)
//        {
//            uint msgId = reader.ReadUInt32();
            
//            Type type;
//            if (!_typeByMsgId.TryGetValue(msgId, out type))
//            {
//                Log.Warn("MessageId:{0} is not registered", msgId);
//                return null;
//            }
//            Message msg = New(msgId, type);
//            //Message msg = (Message)FormatterServices.GetUninitializedObject(type);

//            msg.Deserialize(reader);


//            return msg;
//        }
//    }
}
