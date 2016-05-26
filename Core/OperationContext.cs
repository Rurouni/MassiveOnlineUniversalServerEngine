using System;
using System.Collections.Generic;
using System.Diagnostics;
using MOUSE.Core.Interfaces;

namespace MOUSE.Core
{
    public class OperationContext : IOperationContext
    {
        Dictionary<Type, object> _attachedContexts;


        //correlation, security, identity etc

        public OperationContext(INetChannel replyChannel, Message message, Guid? activityId = null)
        {
            ReplyChannel = replyChannel;
            Message = message;
            var opHeader = message.GetHeader<OperationHeader>();
            

            if (activityId == null)
                ActivityId = opHeader?.ActivityId ?? Guid.NewGuid();
            else
                ActivityId = activityId.Value;    

            RequestId = opHeader?.RequestId;
            ReplyRequired = opHeader != null && opHeader.Type == OperationType.Request;

            _attachedContexts = null;
            Timer = Stopwatch.StartNew();
        }

        public INetChannel ReplyChannel { get; }

        public Message Message { get; }

        public Guid ActivityId { get; }
        public Guid? RequestId { get; }
        public bool ReplyRequired { get; }

        public Stopwatch Timer { get; }

        public TCustomContext Get<TCustomContext>()
        {
            if (_attachedContexts == null)
                return default(TCustomContext);

            object context;
            if (_attachedContexts.TryGetValue(typeof (TCustomContext), out context))
                return (TCustomContext) context;
            else
                return default(TCustomContext);
        }

        public void Set<TCustomContext>(TCustomContext context)
        {
            if (_attachedContexts == null)
                _attachedContexts = new Dictionary<Type, object>();

            _attachedContexts[typeof (TCustomContext)] = context;
        }

        public override string ToString()
        {
            return $"Operation:{{ RequestId:{RequestId}, ActivityId:{ActivityId}, Message:{Message}, Channel:{ReplyChannel}, DurationMs:{Timer.ElapsedMilliseconds} }}";
        }
    }
}