using System;

namespace MOUSE.Core.Interfaces
{
    public interface IOperationContext
    {
        INetChannel ReplyChannel { get; }
        Message Message { get; }
        Guid ActivityId { get; }
        Guid? RequestId { get; }
        bool ReplyRequired { get; }

        TCustomContext Get<TCustomContext>();
        void Set<TCustomContext>(TCustomContext context);
    }
}