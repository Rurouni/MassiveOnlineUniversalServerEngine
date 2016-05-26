using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces.Fibers;

namespace MOUSE.Core.Fibers
{
    public class PollingFiber : IFiber
    {
        volatile bool _isStopped = false;

        readonly ConcurrentQueue<Action> _manualProcessingQueue = new ConcurrentQueue<Action>();

        public void Process(Action func)
        {
            _manualProcessingQueue.Enqueue(func);
        }
        
        public void ExecuteAllInplace()
        {
            if (!_isStopped)
            {
                Action action;
                int limit = 0;
                while (limit++ < 10000 && _manualProcessingQueue.TryDequeue(out action))
                    action();
            }
        }

        public void ProcessAsync(Func<Task> func, LockType lockType = LockType.Write)
        {
            if (_isStopped)
                throw new TaskCanceledException("Fiber was stopped");
            _manualProcessingQueue.Enqueue(() => func().Wait());
        }

        public void Stop()
        {
            _isStopped = true;
        }
    }
}