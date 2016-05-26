using System;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces.Fibers;
using MOUSE.Core.Misc;

namespace MOUSE.Core.Fibers
{
    static public class FiberExt
    {
        static public void Process(this IFiber fiber, Action func, LockType lockType = LockType.Write)
        {
            fiber.ProcessAsync(() =>
            {
                func();
                return Task.FromResult(0);
            }, lockType);

        }

        async static public void Shedule(this IFiber fiber, Func<Task> func, TimeSpan dueTime, LockType lockType = LockType.Write)
        {
            await Task.Delay(dueTime);
            fiber.ProcessAsync(func, lockType);
        }

        async static public void Shedule(this IFiber fiber, Action func, TimeSpan dueTime, LockType lockType = LockType.Write)
        {
            await Task.Delay(dueTime);
            fiber.Process(func, lockType);
        }

        static public Task<TRetType> Call<TRetType>(this IFiber fiber, Func<Task<TRetType>> func, LockType lockType = LockType.Write)
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
            fiber.ProcessAsync(wrapFunc, lockType);
            return tcs.Task;
        }

        static public Task<TRetType> Call<TRetType>(this IFiber fiber, Func<TRetType> func, LockType lockType = LockType.Write)
        {
            var tcs = new TaskCompletionSource<TRetType>();
            Func<Task> wrapFunc = () =>
            {
                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                return TaskExt.Done;
            };
            fiber.ProcessAsync(wrapFunc, lockType);
            return tcs.Task;
        }

        static public Task ContinueOn(this IFiber fiber)
        {
            var tcs = new TaskCompletionSource<object>();
            fiber.ProcessAsync(() =>
            {
                tcs.SetResult(null);
                return tcs.Task;
            });
            return tcs.Task;
        }
    }
}