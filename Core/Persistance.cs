using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOUSE.Core
{
    public interface IPersistanceProvider
    {
        Task<NodeEntity> Get(ulong entityId);
        Task Put(NodeEntity entity);
        Task Delete(NodeEntity entity);
    }

    public class NullPersistanceProvider : IPersistanceProvider
    {
        public Task Delete(NodeEntity entity)
        {
            var tcs = new TaskCompletionSource<NodeEntity>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public Task<NodeEntity> Get(ulong entityId)
        {
            var tcs = new TaskCompletionSource<NodeEntity>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public Task Put(NodeEntity entity)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }
    }
}
