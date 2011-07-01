using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Reflection;

namespace MOUSE.Core
{
    public class NodeEntity
    {
    }

    public class NodeEntityType
    {
        private static readonly Dictionary<Type, NodeEntityType> _neTypesByType;
        private static readonly Dictionary<int, Type> _typesByNEtype;
        private static readonly Dictionary<int, NodeEntityType> _neTypesByHash;
        private static readonly List<int> _usedIds;

        private int _val;
        private Type _type;

        public NodeEntityType()
        {
        }

        private NodeEntityType(int val)
        {
            _val = val;
            _type = null;
        }

        //full copy of .Net hash algo to stabilize it because server and client frameworks are using different algos
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe public static int GenerateHash(string str)
        {
            int ret;
            fixed (char* chrs = str.ToCharArray())
            {
                char* chPtr = chrs;
                int num = 0x15051505;
                int num2 = num;
                int* numPtr = (int*)chPtr;
                for (int i = str.Length; i > 0; i -= 4)
                {
                    num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
                    if (i <= 2)
                    {
                        break;
                    }
                    num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
                    numPtr += 2;
                }

                ret = (num + (num2 * 0x5d588b65));
            }
            if (_usedIds.Contains((ushort)ret))
            {
                throw new Exception("Hash overlapp occurred!!!!");
            }
            _usedIds.Add(ret);
            return ret;

        }

        public int Value
        {
            get { return _val; }
        }

        public Type Type
        {
            get
            {
                if (_type == null)
                    _type = GetType(_val);
                return _type;
            }
        }

        static NodeEntityType()
        {
            _neTypesByType = new Dictionary<Type, NodeEntityType>();
            _neTypesByHash = new Dictionary<int, NodeEntityType>();
            _typesByNEtype = new Dictionary<int, Type>();
            _usedIds = new List<int>();

            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.ContainsAttribute<NodeEntityContractAttribute>())
                {
                    int val = GenerateHash(type.FullName);
                    var neType = new NodeEntityType(val);
                    _neTypesByType.Add(type, neType);
                    _typesByNEtype.Add(val, type);
                    _neTypesByHash.Add(val, neType);
                }
            }
        }

        public static implicit operator int(NodeEntityType type)
        {
            return type.Value;
        }

        public static implicit operator NodeEntityType(int id)
        {
            return _neTypesByHash[id];
        }

        public static NodeEntityType GetEntityType(Type t)
        {
            return _neTypesByType[t];
        }

        public static IEnumerable<NodeEntityType> GetEntityTypes()
        {
            return _neTypesByType.Values;
        }

        public static Type GetType(NodeEntityType t)
        {
            Type type = null;

            if (!_typesByNEtype.TryGetValue(t, out type))
                throw new Exception(string.Format("Type with ne_type_key:{0} has not found", t.Value));

            return type;
        }

        public static NodeEntityType Get<T>() where T : class
        {
            return _neTypesByType[typeof(T)];
        }

        public static NodeEntityType Get(Type type)
        {
            NodeEntityType obj;
            _neTypesByType.TryGetValue(type, out obj);
            return obj;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;
            if (!(obj is NodeEntityType))
                return false;

            return _val == ((NodeEntityType)obj)._val;
        }

        public static bool operator ==(NodeEntityType o1, NodeEntityType o2)
        {
            if (ReferenceEquals(o1, null) && ReferenceEquals(o2, null))
                return true;
            if (ReferenceEquals(o1, null) ^ ReferenceEquals(o2, null))
                return false;
            return o1._val == o2._val;
        }

        public static bool operator !=(NodeEntityType o1, NodeEntityType o2)
        {
            return !(o1 == o2);
        }

        public override int GetHashCode()
        {
            return _val;
        }

        public override string ToString()
        {
            if (Type == null)
                return "UNKNOWN";
            return Type.Name;
        }
    }

    public struct NodeEntityKey
    {
        public NodeEntityType Type;
        public uint Id;

        public NodeEntityKey(NodeEntityType type, uint id)
        {
            Type = type;
            Id = id;
        }

        public int TypeVal
        {
            get { return Type != null ? Type.Value : 0; }
        }

        public string TypeName
        {
            get { return Type != null ? Type.Type.Name : ""; }
        }

        public override bool Equals(object obj)
        {
            var key = (NodeEntityKey)obj;
            return Type.Equals(key.Type) && Id.Equals(key.Id);
        }

        public override int GetHashCode()
        {
            return (int)(Type ^ Id);
        }

        public static long ConvertToLong(NodeEntityKey key)
        {
            return key.Id ^ ((long)key.Type << 32);
        }

        public static long ConvertToLong(uint id, NodeEntityType type)
        {
            return id ^ ((long)type << 32);
        }

        public static implicit operator long(NodeEntityKey key)
        {
            return ConvertToLong(key);
        }

        public static implicit operator NodeEntityKey(long fullId)
        {
            return ConvertFromLong(fullId);
        }

        public static NodeEntityKey ConvertFromLong(long key)
        {
            return new NodeEntityKey((NodeEntityType)(key >> 32), (uint)(key & 0xffffffff));
        }
    }
}
