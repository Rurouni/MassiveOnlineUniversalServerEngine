using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MOUSE.Core
{
    public class ServerFiber
    {
        private readonly ConcurrentExclusiveSchedulerPair _schedulerPair = new ConcurrentExclusiveSchedulerPair();
        private readonly ActionBlock<Func<Task>> _writeLockedQueue;
        private readonly ActionBlock<Func<Task>> _readLockedQueue;

        public ServerFiber()
        {
            _readLockedQueue = new ActionBlock<Func<Task>>((Func<Func<Task>, Task>)ProcessFunc,
                                                           new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ConcurrentScheduler, MaxDegreeOfParallelism = 10 });
            _writeLockedQueue = new ActionBlock<Func<Task>>((Func<Func<Task>, Task>)ProcessFunc,
                                                            new ExecutionDataflowBlockOptions { TaskScheduler = _schedulerPair.ExclusiveScheduler });
        }

        private async Task ProcessFunc(Func<Task> func)
        {
            await func();
        }

        public void Process(Action func, LockType lockType)
        {
            ProcessAsync(() =>
                {
                    func();
                    return Task.FromResult(0);
                }, lockType);

        }

        public void ProcessAsync(Func<Task> func, LockType lockType)
        {
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

        public async void Shedule(Func<Task> func, TimeSpan dueTime, LockType lockType)
        {
            await Task.Delay(dueTime);
            ProcessAsync(func, lockType);
        }

        public async void Shedule(Action func, TimeSpan dueTime, LockType lockType)
        {
            await Task.Delay(dueTime);
            Process(func, lockType);
        }

        public Task<TRetType> Call<TRetType>(Func<Task<TRetType>> func, LockType lockType)
        {
            var tcs = new TaskCompletionSource<TRetType>();
            Func<Task> wrapFunc = async () =>
                {
                    try
                    {
                        var result = await func();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                };
            ProcessAsync(wrapFunc, lockType);
            return tcs.Task;
        }
    }
}