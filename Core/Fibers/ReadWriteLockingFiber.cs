using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MOUSE.Core.Interfaces.Fibers;

namespace MOUSE.Core.Fibers
{
    public class ReadWriteLockingFiber : IFiber
    {
        private readonly ConcurrentExclusiveSchedulerPair _schedulerPair = new ConcurrentExclusiveSchedulerPair();
        private readonly ActionBlock<Func<Task>> _writeLockedQueue;
        private readonly ActionBlock<Func<Task>> _readLockedQueue;
        volatile bool _isStopped = false;

        public ReadWriteLockingFiber(int readParallelism = 10, int maxQueueSize = 10000)
        {
            _readLockedQueue = new ActionBlock<Func<Task>>(ProcessFunc, new ExecutionDataflowBlockOptions
                                {
                                    TaskScheduler = _schedulerPair.ConcurrentScheduler,
                                    MaxDegreeOfParallelism = readParallelism,
                                    BoundedCapacity = maxQueueSize
                                });
            _writeLockedQueue = new ActionBlock<Func<Task>>(ProcessFunc, new ExecutionDataflowBlockOptions
                                {
                                    TaskScheduler = _schedulerPair.ExclusiveScheduler,
                                    BoundedCapacity = maxQueueSize
                                });
        }

        private Task ProcessFunc(Func<Task> func)
        {
            if (_isStopped)
                throw new TaskCanceledException("Fiber was stopped");
            return func();
        }

        public void ProcessAsync(Func<Task> func, LockType lockType)
        {
            if (_isStopped)
                throw new TaskCanceledException("Fiber was stopped");

            switch (lockType)
            {
                case LockType.None:
                    func();
                    break;
                case LockType.Read:
                    _readLockedQueue.Post(func);
                    break;
                case LockType.Write:
                    _writeLockedQueue.Post(func);
                    break;
            }
        }

        public void Stop()
        {
            _isStopped = true;
            _writeLockedQueue.Complete();
            _readLockedQueue.Complete();
        }
    }
}