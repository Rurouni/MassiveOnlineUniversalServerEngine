using System;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.Actors
{
    public class DelegatingActorProxy : IActorProxy
    {
        readonly Func<IOperationContext, Message, Task<Message>> _sendAsync;
        readonly Action<IOperationContext, Message> _sendOneWay;

        public ActorRef ActorRef { get; }

        public IObservable<IActorProxy> OnDisconnected { get; }

        public DelegatingActorProxy(ActorRef actorRef,
            Func<IOperationContext, Message, Task<Message>> sendAsync,
            Action<IOperationContext, Message> sendOneWay,
            IObservable<IActorProxy> onDisconnected)
        {
            ActorRef = actorRef;
            _sendAsync = sendAsync;
            _sendOneWay = sendOneWay;
            OnDisconnected = onDisconnected;
        }

        async public Task<T> SendAsync<T>(Message request, IOperationContext context) where T : Message
        {
            var reply = await _sendAsync(context, request);
            return (T)reply;
        }

        public void SendOneWay(Message msg, IOperationContext context)
        {
            _sendOneWay(context, msg);
        }
    }

    
}