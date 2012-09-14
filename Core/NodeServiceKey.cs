using System.IO;
namespace MOUSE.Core
{
    public static class NodeServiceKeySerializer
    {
        public static void Serialize(NodeServiceKey x, BinaryWriter w)
        {
            w.Write(x.TypeId);
            w.Write(x.Id);
        }

        public static NodeServiceKey Deserialize(BinaryReader r)
        {
            uint typeId = r.ReadUInt32();
            uint id = r.ReadUInt32();
            return new NodeServiceKey(typeId, id);
        }
    }

    public class NodeServiceKey
    {
        public readonly uint TypeId;
        public readonly uint Id;

        public NodeServiceKey(uint typeId, uint id)
        {
            TypeId = typeId;
            Id = id;
        }

        public NodeServiceKey(BinaryReader r)
        {
            TypeId = r.ReadUInt32();
            Id = r.ReadUInt32();
        }

        public override bool Equals(object obj)
        {
            var key = (NodeServiceKey)obj;
            return TypeId.Equals(key.TypeId) && Id.Equals(key.Id);
        }

        public override int GetHashCode()
        {
            return (int)(TypeId ^ Id);
        }

        public void Serialize(BinaryWriter w)
        {
            w.Write(TypeId);
            w.Write(Id);
        }

        public override string ToString()
        {
            return string.Format("ServiceKey<TypeId: {0}, Id: {1}>", TypeId, Id);
        }

        //public static long ConvertToLong(NodeServiceKey key)
        //{
        //    return key.Id ^ ((long)key.TypeId << 32);
        //}

        //public static long ConvertToLong(uint id, uint typeId)
        //{
        //    return id ^ ((long)type << 32);
        //}

        //public static implicit operator long(NodeServiceKey key)
        //{
        //    return ConvertToLong(key);
        //}

        //public static implicit operator NodeServiceKey(long fullId)
        //{
        //    return ConvertFromLong(fullId);
        //}

        //public static NodeServiceKey ConvertFromLong(long key)
        //{
        //    return new NodeServiceKey((uint)key >> 32, (uint)(key & 0xffffffff));
        //}
    }
}