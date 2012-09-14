using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOUSE.Core
{
    public interface IPersistanceProvider
    {
        Task<NodeService> Get(NodeServiceKey serviceKey);
        Task Put(NodeService entity);
        Task Delete(NodeService entity);
    }

    public class NullPersistanceProvider : IPersistanceProvider
    {
        public Task Delete(NodeService entity)
        {
            var tcs = new TaskCompletionSource<NodeService>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public Task<NodeService> Get(NodeServiceKey serviceKey)
        {
            var tcs = new TaskCompletionSource<NodeService>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public Task Put(NodeService entity)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }
    }
}
