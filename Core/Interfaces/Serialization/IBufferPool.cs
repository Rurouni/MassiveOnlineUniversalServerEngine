namespace MOUSE.Core.Interfaces.Serialization
{
    public interface IBufferPool
    {
        byte[] Take(int size);
        void Return(byte[] buffer);
    }
}