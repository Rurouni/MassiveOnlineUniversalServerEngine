using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOUSE.Core
{
    public interface IPersistanceProvider
    {
        Task<NodeEntity> LoadAsync(ulong entityId);

        Task Delete(NodeEntity entity);
    }

    public class MembasePersistance : IPersistanceProvider
    {
        public Task<NodeEntity> LoadAsync(ulong entityId)
        {
            TaskCompletionSource<NodeEntity> tcs = new TaskCompletionSource<NodeEntity>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public Task Delete(NodeEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
