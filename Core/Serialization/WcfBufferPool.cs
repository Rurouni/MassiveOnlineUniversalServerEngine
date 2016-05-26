using System.ServiceModel.Channels;
using MOUSE.Core.Interfaces.Serialization;

namespace MOUSE.Core.Serialization
{
    public class WcfBufferPool : IBufferPool
    {
        readonly BufferManager _internalPool = BufferManager.CreateBufferManager(500 * 1024 * 1024, 10*1024 * 1024);
        public byte[] Take(int size)
        {
            return _internalPool.TakeBuffer(size);
        }

        public void Return(byte[] buffer)
        {
            _internalPool.ReturnBuffer(buffer);
        }
    }
}