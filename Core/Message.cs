using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MOUSE.Core
{
    [DataContract]
    public abstract class Message
    {
        [DataMember]
        private List<MessageHeader> _headers;

        public abstract uint Id {get;}
        
        public virtual MessagePriority Priority
        {
            get { return MessagePriority.Medium; }
        }

        public virtual MessageReliability Reliability
        {
            get { return MessageReliability.ReliableOrdered; }
        }

        public void AttachHeader(MessageHeader header)
        {
            if (_headers == null)
                _headers = new List<MessageHeader>();
            _headers.Add(header);
        }

        public THeader GetHeader<THeader>() where THeader : MessageHeader
        {
            if (_headers == null)
                return default(THeader);

            return (THeader)_headers.FirstOrDefault(x => x is THeader);
        }

        public void RemoveHeader<THeader>() where THeader : MessageHeader
        {
            if (_headers == null)
                return;

            _headers.RemoveAll(x => x is THeader);
        }

        public virtual void Serialize(NativeWriter writer)
        {
            writer.Write(Id);
            writer.Write((byte)(_headers != null ? _headers.Count : 0));
            if (_headers != null)
                foreach (var header in _headers)
                    MessageHeader.Serialize(header, writer);
        }

        public virtual void Deserialize(NativeReader reader)
        {
            int count = reader.ReadByte();
            if (count > 0)
            {
                _headers = new List<MessageHeader>(count);
                while (count > 0)
                {
                    _headers.Add(MessageHeader.Deserialize(reader));
                    count--;
                }
            }
        }
    }
}
