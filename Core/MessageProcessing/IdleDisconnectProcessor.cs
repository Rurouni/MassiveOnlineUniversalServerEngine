using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.MessageProcessing;

namespace MOUSE.Core.MessageProcessing
{
    public class IdleDisconnectProcessor : IGenericMessageProcessor
    {
        readonly TimeSpan _idleTimeout;
        DateTime _lastReceivedTime;
        INetChannel _channel;

        public IdleDisconnectProcessor(TimeSpan idleTimeout, INetChannel channel)
        {
            _lastReceivedTime = DateTime.UtcNow;
            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => KillIfIdle());
            _idleTimeout = idleTimeout;
            _channel = channel;
        }

        void KillIfIdle()
        {
            if(DateTime.UtcNow.Subtract(_idleTimeout) > _lastReceivedTime)
                _channel.Dispose();
                
        }

        public Task<Message> Process(Func<IOperationContext, Task<Message>> next, IOperationContext operation)
        {
            _lastReceivedTime = DateTime.UtcNow;
            return next(operation);
        }
    }
}