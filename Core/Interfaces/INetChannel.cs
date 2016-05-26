using System;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;

namespace MOUSE.Core.Interfaces
{
    public interface INetChannel : IOneWayChannel
    {
        IObservable<INetChannel> DisconnectedEvent { get; }
        IObservable<OperationContext> OperationReceivedEvent { get; }
        bool IsConnected { get; }

        Task<Message> SendRequestAsync(Message request);
        Task<TReply> SendRequestAsync<TReply>(Message request) where TReply : Message;

        void ReplyWithError(IOperationContext context, ushort errorCode, string errorDesc);
        void Reply(IOperationContext context, Message replyMsg);

        IDisposable RegisterHandler<TMessage>(Action<TMessage> messageHandler) where TMessage : Message;
    }

    public interface IOneWayChannel : IDisposable
    {
        ITransportChannel TransportChannel { get; }

        void Send(Message msg);
    }
}