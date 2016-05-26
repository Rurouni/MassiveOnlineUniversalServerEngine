using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;

namespace MOUSE.Core
{
    [Serializable]
    [DataContract]
    public abstract class Message
    {
        [DataMember(Order = 1)]
        List<MessageHeader> _headers;
        
        public virtual MessagePriority Priority => MessagePriority.Medium;

        public virtual MessageReliability Reliability => MessageReliability.Reliable;

        public void AttachHeader<THeader>(THeader header) where THeader : MessageHeader
        {
            if (_headers == null)
            {
                _headers = new List<MessageHeader> { header};
            }
            else
            {
                RemoveHeader<THeader>();
                _headers.Add(header);
            }
        }

        public THeader GetHeader<THeader>() where THeader : MessageHeader
        {
            if (_headers == null)
                return null;
            foreach (var header in _headers)
            {
                var target = header as THeader;
                if (target != null)
                    return target;
            }
            return null;
        }

        public void RemoveHeader<THeader>() where THeader : MessageHeader
        {
            _headers?.RemoveAll(x => x is THeader);
        }

        public void ClearHeaders()
        {
            _headers?.Clear();
        }
    }
}
