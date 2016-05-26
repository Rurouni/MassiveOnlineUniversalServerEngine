using System;
using System.Threading.Tasks;

namespace MOUSE.Core.Interfaces.Fibers
{
    public interface IFiber
    {
        void ProcessAsync(Func<Task> func, LockType lockType = LockType.Write);
        void Stop();
    }
}