using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MOUSE.Core.Interfaces.Fibers;

namespace MOUSE.Core.Fibers
{
    public class SimpleFiber : IFiber
    {
        readonly ActionBlock<Func<Task>> _processingQueue;
        volatile bool _isStopped = false;

        public SimpleFiber(TaskScheduler scheduler, int maxDegreeOfParallelism = 1, int maxQueueSize = 10000)
        {
            _processingQueue = new ActionBlock<Func<Task>>(func => func(),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = scheduler,
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    BoundedCapacity = maxQueueSize
                });
        }

        public SimpleFiber() : this(TaskScheduler.Default)
        {
        }

        public void ProcessAsync(Func<Task> func, LockType lockType = LockType.Write)
        {
            _processingQueue.Post(func);
        }

        public void Stop()
        {
            _isStopped = true;
            _processingQueue.Complete();
        }
    }
}