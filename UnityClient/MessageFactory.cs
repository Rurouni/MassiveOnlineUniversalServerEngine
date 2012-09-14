using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using UnityClient;
using System.IO;


namespace MOUSE.Core
{
    public interface IMessageFactory
    {
        TMessage New<TMessage>() where TMessage : Message;
        void Free(Message msg);
        Message Deserialize(BinaryReader reader);
    }

    //TODO: rework headers sp they also could be pooled
    public class MessageFactory : IMessageFactory
    {
        //readonly Logger Log = LogManager.GetCurrentClassLogger();
        readonly Logger Log = new Logger();
        private Dictionary<Type, uint> _msgIdByType = new Dictionary<Type,uint>();
        private Dictionary<uint, Type> _typeByMsgId = new Dictionary<uint,Type>();
        private Dictionary<uint, Stack<Message>> _messagePoolByMsgId = new Dictionary<uint, Stack<Message>>();

        
        public MessageFactory(IEnumerable<Message> importedMessages)
        {
            foreach (var msg in importedMessages)
            {
                Type type = msg.GetType();
                _msgIdByType.Add(type, msg.Id);
                _typeByMsgId.Add(msg.Id, type);
                var pool = new Stack<Message>();
                pool.Push(msg);
                _messagePoolByMsgId[msg.Id] = pool;
                Log.Info("Registered Message<Id:{0} Type:{1}>", msg.Id, type);
            }
        }

        public TMessage New<TMessage>() where TMessage : Message
        {
            uint msgId;
            if(!_msgIdByType.TryGetValue(typeof(TMessage), out msgId))
                throw new Exception(string.Format("{0} is not registered message", typeof(TMessage).Name));

           return (TMessage)New(msgId, typeof(TMessage));
        }

        private Message New(uint msgId, Type type)
        {
            Stack<Message> pool = _messagePoolByMsgId[msgId];
            Message msg;
            if(pool.Count > 0)
            {
                return pool.Pop();
            }
            else
            {
                msg = (Message)FormatterServices.GetUninitializedObject(type);
                pool.Push(msg);
                Log.Debug("Created new {0} in pool", msg, type.Name);
                return msg;
            }
        }

        public void Free(Message msg)
        {
            msg.ClearHeaders();
            _messagePoolByMsgId[msg.Id].Push(msg);
        }

        public Message Deserialize(BinaryReader reader)
        {
            uint msgId = reader.ReadUInt32();
            
            Type type;
            if(!_typeByMsgId.TryGetValue(msgId, out type))
                throw new Exception(string.Format("MessageId:{0} is not registered", msgId));
            //Message msg = New(msgId, type);
            Message msg = (Message)FormatterServices.GetUninitializedObject(type);
            msg.Deserialize(reader);
            return msg;
        }
    }
}
