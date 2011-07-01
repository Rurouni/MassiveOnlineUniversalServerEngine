using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Core
{
    public interface IPersistanceProvider
    {
        Task<NodeEntity> LoadAsync(ulong entityId);
    }

    public class MembasePersistance : IPersistanceProvider
    {
        public Task<NodeEntity> LoadAsync(ulong entityId)
        {
            TaskCompletionSource<NodeEntity> tcs = new TaskCompletionSource<NodeEntity>();
            tcs.SetResult(null);
            return tcs.Task;
        }
    }
}
