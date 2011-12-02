using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MOUSE.Core
{
    public class OperationContext
    {
        public readonly Message Message;
        public readonly INetPeer Source;

        public OperationContext(Message message, INetPeer source)
        {
            Message = message;
            Source = source;
        }
    }

    public interface INodeService
    {
        OperationContext Context { get; set; }
        NodeServiceDescription Description { get; }
    }

    public class NodeService : INodeService
    {
        NodeServiceDescription _description;
        protected Logger Log;
        public ServerFiber<Message> Fiber;
        protected ServerNode Node;
        private uint _id;

        public uint Id
        {
            get { return _id; }
        }


        /// <summary>
        /// Any async method using this should be aware that Context is not restored in continuations, so only LockType.Full garanties Context remains the same,
        ///  or you can save Context in stack variable(or clojure)
        /// </summary>
        public OperationContext Context { get;set; }

        public void Init(uint id, NodeServiceDescription desc, ServerNode node)
        {
            _id = id;
            _description = desc;
            Node = node;
            Log = LogManager.GetLogger(string.Format("{0}<Id:{1}>", GetType().Name, id));
            Fiber = new ServerFiber<Message>();
        }

        public NodeServiceDescription Description { get { return _description; } }

        public Task<Message> ProcessMessage(OperationContext operationContext)
        {
            return Fiber.ProcessAndReturn(() => DispatchAndReturnAsync(operationContext), operationContext.Message.LockType);
        }

        private Task<Message> DispatchAndReturnAsync(OperationContext context)
        {
            try
            {
                //TODO: research how to make this context propagate on async continuations
                Context = context;
                return Node.Protocol.Dispatch(this, context.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw new Exception("Error on DispatchAndReturnAsync", ex);
            }
        }
    }
    
    public class NodeServiceDescription
    {
        public readonly NodeServiceAttribute Attribute;
        public readonly List<NodeServiceContractDescription> ImplementedContracts;
        public readonly Type ServiceType;

        public NodeServiceDescription(Type serviceType, IEnumerable<NodeServiceContractDescription> contracts, NodeServiceAttribute attribute)
        {
            Attribute = attribute;
            ImplementedContracts = contracts.ToList();
            ServiceType = serviceType;
        }

        public bool Persistant 
        {
            get { return Attribute.Persistant; }
        }
        public bool AutoCreate 
        { 
            get { return Attribute.AutoCreate; }
        }
    }

    public class NodeServiceContractDescription
    {
        public readonly uint TypeId;
        public readonly Type ContractType;
        public readonly Type ProxyType;
        public readonly List<NodeServiceOperationDescription> Operations;
        public readonly NetContractAttribute Attribute;

        public NodeServiceContractDescription(uint typeId, Type contractType, Type proxyType,
            NetContractAttribute contractAttribute, List<NodeServiceOperationDescription> operations)
        {
            TypeId = typeId;
            ContractType = contractType;
            ProxyType = proxyType;
            Attribute = contractAttribute;
            Operations = operations;
        }

        public bool AllowExternalConnections
        {
            get { return Attribute.AllowExternalConnections; }
        }
    }

    public class NodeServiceOperationDescription
    {
        public readonly string Name;
        public readonly uint RequestMessageId;
        public readonly uint? ReplyMessageId;
        public readonly Func<IMessageFactory, object, Message, Task<Message>> Dispatch;

        public NodeServiceOperationDescription(string name, uint requestMessageId, uint? replyMessageId, Func<IMessageFactory, object, Message, Task<Message>> dispatch)
        {
            Name = name;
            RequestMessageId = requestMessageId;
            ReplyMessageId = replyMessageId;
            Dispatch = dispatch;
        }
    }

    public interface INodeServiceProxy
    {
        NodeServiceKey ServiceKey { get; }
        INetPeer RemoteTarget { get; }
        IMessageFactory MessageFactory { get; }
        NodeService DirectTarget { get; }
        NodeServiceContractDescription Description { get; }
        Task<Message> ExecuteServiceOperation(Message request);
        void ExecuteOneWayServiceOperation(Message request);
    
    }

    public abstract class NodeServiceProxy : INodeServiceProxy
    {
        private NodeServiceKey _serviceKey;
        private NodeServiceContractDescription _description;
        private ServiceHeader _serviceHeader;
        private INetPeer _remoteTarget;
        private NodeService _directTarget;

        public void Init(NodeServiceKey serviceKey, NodeServiceContractDescription description, INetPeer remoteTarget, NodeService directTarget = null)
        {
            _serviceKey = serviceKey;
            _description = description;
            _serviceHeader = new ServiceHeader(serviceKey);
            _remoteTarget = remoteTarget;
            _directTarget = directTarget;
        }

        public NodeServiceKey ServiceKey
        {
            get { return _serviceKey; }
        }

        public INetPeer RemoteTarget
        {
            get { return _remoteTarget; }
        }

        public IMessageFactory MessageFactory
        {
            get { return _remoteTarget.MessageFactory; }
        }

        public NodeService DirectTarget
        {
            get { return _directTarget; }
        }

        public NodeServiceContractDescription Description
        {
            get { return _description; }
        }

        public Task<Message> ExecuteServiceOperation(Message request)
        {
            request.AttachHeader(_serviceHeader);
            if (DirectTarget != null)
                return DirectTarget.ProcessMessage(new OperationContext(request, null));
            else
                return RemoteTarget.ExecuteOperation(request);
        }

        public void ExecuteOneWayServiceOperation(Message request)
        {
            request.AttachHeader(_serviceHeader);
            if (DirectTarget != null)
                DirectTarget.ProcessMessage(new OperationContext(request, null));
            else
                RemoteTarget.Channel.Send(request);
        }
    }



    //public class NodeEntityType
    //{
    //    private static readonly Dictionary<Type, NodeEntityType> _neTypesByType;
    //    private static readonly Dictionary<int, Type> _typesByNEtype;
    //    private static readonly Dictionary<int, NodeEntityType> _neTypesByHash;
    //    private static readonly List<int> _usedIds;

    //    private int _val;
    //    private Type _type;

    //    public NodeEntityType()
    //    {
    //    }

    //    private NodeEntityType(int val)
    //    {
    //        _val = val;
    //        _type = null;
    //    }

    //    //full copy of .Net hash algo to stabilize it because server and client frameworks are using different algos
    //    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    //    unsafe public static int GenerateHash(string str)
    //    {
    //        int ret;
    //        fixed (char* chrs = str.ToCharArray())
    //        {
    //            char* chPtr = chrs;
    //            int num = 0x15051505;
    //            int num2 = num;
    //            int* numPtr = (int*)chPtr;
    //            for (int i = str.Length; i > 0; i -= 4)
    //            {
    //                num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
    //                if (i <= 2)
    //                {
    //                    break;
    //                }
    //                num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
    //                numPtr += 2;
    //            }

    //            ret = (num + (num2 * 0x5d588b65));
    //        }
    //        if (_usedIds.Contains((ushort)ret))
    //        {
    //            throw new Exception("Hash overlapp occurred!!!!");
    //        }
    //        _usedIds.Add(ret);
    //        return ret;

    //    }

    //    public int Value
    //    {
    //        get { return _val; }
    //    }

    //    public Type Type
    //    {
    //        get
    //        {
    //            if (_type == null)
    //                _type = GetType(_val);
    //            return _type;
    //        }
    //    }

    //    static NodeEntityType()
    //    {
    //        _neTypesByType = new Dictionary<Type, NodeEntityType>();
    //        _neTypesByHash = new Dictionary<int, NodeEntityType>();
    //        _typesByNEtype = new Dictionary<int, Type>();
    //        _usedIds = new List<int>();

    //        Assembly assembly = Assembly.GetExecutingAssembly();
    //        foreach (Type type in assembly.GetTypes())
    //        {
    //            if (type.ContainsAttribute<NodeEntityContractAttribute>())
    //            {
    //                int val = GenerateHash(type.FullName);
    //                var neType = new NodeEntityType(val);
    //                _neTypesByType.Add(type, neType);
    //                _typesByNEtype.Add(val, type);
    //                _neTypesByHash.Add(val, neType);
    //            }
    //        }
    //    }

    //    public static implicit operator int(NodeEntityType type)
    //    {
    //        return type.Value;
    //    }

    //    public static implicit operator NodeEntityType(int id)
    //    {
    //        return _neTypesByHash[id];
    //    }

    //    public static NodeEntityType GetEntityType(Type t)
    //    {
    //        return _neTypesByType[t];
    //    }

    //    public static IEnumerable<NodeEntityType> GetEntityTypes()
    //    {
    //        return _neTypesByType.Values;
    //    }

    //    public static Type GetType(NodeEntityType t)
    //    {
    //        Type type = null;

    //        if (!_typesByNEtype.TryGetValue(t, out type))
    //            throw new Exception(string.Format("Type with ne_type_key:{0} has not found", t.Value));

    //        return type;
    //    }

    //    public static NodeEntityType Get<T>() where T : class
    //    {
    //        return _neTypesByType[typeof(T)];
    //    }

    //    public static NodeEntityType Get(Type type)
    //    {
    //        NodeEntityType obj;
    //        _neTypesByType.TryGetValue(type, out obj);
    //        return obj;
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (ReferenceEquals(obj, null))
    //            return false;
    //        if (!(obj is NodeEntityType))
    //            return false;

    //        return _val == ((NodeEntityType)obj)._val;
    //    }

    //    public static bool operator ==(NodeEntityType o1, NodeEntityType o2)
    //    {
    //        if (ReferenceEquals(o1, null) && ReferenceEquals(o2, null))
    //            return true;
    //        if (ReferenceEquals(o1, null) ^ ReferenceEquals(o2, null))
    //            return false;
    //        return o1._val == o2._val;
    //    }

    //    public static bool operator !=(NodeEntityType o1, NodeEntityType o2)
    //    {
    //        return !(o1 == o2);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return _val;
    //    }

    //    public override string ToString()
    //    {
    //        if (Type == null)
    //            return "UNKNOWN";
    //        return Type.Name;
    //    }
    //}

    public class NodeServiceKey
    {
        public readonly uint TypeId;
        public readonly uint Id;

        public NodeServiceKey(uint typeId, uint id)
        {
            TypeId = typeId;
            Id = id;
        }

        public NodeServiceKey(NativeReader r)
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

        public void Serialize(NativeWriter w)
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

    public static class NodeServiceKeySerializer
    {
        public static void Serialize(NodeServiceKey x, NativeWriter w)
        {
            w.Write(x.TypeId);
            w.Write(x.Id);
        }

        public static NodeServiceKey Deserialize(NativeReader r)
        {
            uint typeId = r.ReadUInt32();
            uint id = r.ReadUInt32();
            return new NodeServiceKey(typeId, id);
        }
    }
}
