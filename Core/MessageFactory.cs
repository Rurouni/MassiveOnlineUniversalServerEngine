using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using System.Runtime.Serialization;

namespace MOUSE.Core
{
    public interface IMessageFactory
    {
        TMessage New<TMessage>() where TMessage : Message;
        void Free(Message msg);
        Message Deserialize(NativeReader reader);
    }

    public class ReflectionBasedMessageFactory : IMessageFactory
    {
        Logger Log = LogManager.GetCurrentClassLogger();
        private Dictionary<Type, uint> _msgIdByType = new Dictionary<Type,uint>();
        private Dictionary<uint, Type> _typeByMsgId = new Dictionary<uint,Type>();
        private Dictionary<uint, List<Message>> _messagePoolByMsgId = new Dictionary<uint,List<Message>>();

        public ReflectionBasedMessageFactory()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if(type.IsSubclassOf(typeof(Message)))
                    {
                        Message msg = (Message)FormatterServices.GetUninitializedObject(type);
                        _msgIdByType.Add(type, msg.Id);
                        _typeByMsgId.Add(msg.Id, type);
                        _messagePoolByMsgId.Add(msg.Id, new List<Message> { msg });
                        Log.Info("Registered Message<Id:{0} Type:{1}>", msg.Id, type);
                    }
                }
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
            List<Message> pool = _messagePoolByMsgId[msgId];
            if(pool.Count == 0)
            {
                Message msg = (Message)FormatterServices.GetUninitializedObject(type);
                pool.Add(msg);
                return msg;
            }
            else
            {
                Message msg = pool[pool.Count-1];
                pool.RemoveAt(pool.Count-1);
                return msg;
            }
        }

        public void Free(Message msg)
        {
 	        _messagePoolByMsgId[msg.Id].Add(msg);
        }

        public Message Deserialize(NativeReader reader)
        {
            uint msgId = reader.ReadUInt32();
            
            Type type;
            if(!_typeByMsgId.TryGetValue(msgId, out type))
                throw new Exception(string.Format("MessageId:{0} is not registered", msgId));
            Message msg = New(msgId, type);
            msg.Deserialize(reader);
            return msg;
        }
    }
}
