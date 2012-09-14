using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;

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

        public void AttachHeader<THeader>(THeader header) where THeader : MessageHeader
        {
            if (_headers == null)
                _headers = new List<MessageHeader>();
            
            RemoveHeader<THeader>();
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

        public void ClearHeaders()
        {
            if (_headers == null)
                return;

            _headers.Clear();
        }

        BinaryWriter _writer;
        /// <summary>
        /// TODO: non optimal
        /// </summary>
        public byte[] GetSerialized()
        {
            if (_writer == null)
                _writer = new BinaryWriter(new MemoryStream(128));
            else
                _writer.Seek(0, SeekOrigin.Begin);
            Serialize(_writer);
            long count = _writer.BaseStream.Position;
            _writer.BaseStream.Seek(0, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(_writer.BaseStream);
            return reader.ReadBytes((int)count);
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write((byte)(_headers != null ? _headers.Count : 0));
            if (_headers != null)
                foreach (var header in _headers)
                    MessageHeader.Serialize(header, writer);
        }

        public virtual void Deserialize(BinaryReader reader)
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

        public override string ToString()
        {
            return "Message<" + GetType().Name + ">";
        }
    }
}
