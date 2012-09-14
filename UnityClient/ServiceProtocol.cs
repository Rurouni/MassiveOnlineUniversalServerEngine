using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityClient;

namespace MOUSE.Core
{
    public interface IServiceProtocol
    {
        void DispatchOneWay(object target, Message msg);
        NodeServiceProxy CreateProxy(NodeServiceKey serviceKey, IMessageFactory messageFactory, INetClient client);

        NodeServiceKey GetKey<TServiceContract>(uint serviceId = 0);
        uint GetContractId(Type contractType);
        bool TryGetContractId(Type contractType, out uint typeId);
        Type GetContractType(uint serviceTypeId);
        NodeServiceContractDescription GetDescription(uint serviceTypeId);
    }

    public class ServiceProtocol : IServiceProtocol
    {
        //Logger Log = LogManager.GetCurrentClassLogger();

        Logger Log = new Logger();
        private readonly Dictionary<uint, NodeServiceContractDescription> _descByTypeId = new Dictionary<uint, NodeServiceContractDescription>();
        private readonly Dictionary<uint, Action<IMessageFactory, object, Message>> _dispatcherByMsgId = new Dictionary<uint, Action<IMessageFactory, object, Message>>();
        private readonly Dictionary<Type, uint> _serviceTypeIdByContractType = new Dictionary<Type, uint>();
        private readonly IMessageFactory _messageFactory;

        public ServiceProtocol(IMessageFactory msgfactory, IEnumerable<NodeServiceProxy> importedProxies)
        {
            _messageFactory = msgfactory;

            foreach (var proxy in importedProxies)
            {
                Type type = proxy.GetType();
                if (type.ContainsAttribute<NetProxyAttribute>())
                {
                    var proxyAttr = type.GetAttribute<NetProxyAttribute>();
                    var contractAttr = proxyAttr.ContractType.GetAttribute<NetContractAttribute>();

                    var operations = new List<NodeServiceOperationDescription>();
                    foreach (var method in type.GetMethods().Where(x => x.ContainsAttribute<NetOperationDispatcherAttribute>()))
                    {
                        var opAttr = method.GetAttribute<NetOperationDispatcherAttribute>();
                        Message request = (Message)FormatterServices.GetUninitializedObject(opAttr.RequestMessage);
                        uint? replyId = null;
                        if (opAttr.ReplyMessage != null)
                            replyId = ((Message)FormatterServices.GetUninitializedObject(opAttr.ReplyMessage)).Id;
                        var opDesc = new NodeServiceOperationDescription(
                            method.Name, request.Id, replyId, (Action<IMessageFactory, object, Message>)Delegate.CreateDelegate(
                                                                            typeof(Action<IMessageFactory, object, Message>), method));
                        operations.Add(opDesc);
                    }

                    var desc = new NodeServiceContractDescription(proxyAttr.ContractTypeId,
                        proxyAttr.ContractType, type, contractAttr, operations);
                    RegisterService(desc);

                }
            }
        }

        
        public void RegisterService(NodeServiceContractDescription desc)
        {
            _descByTypeId.Add(desc.TypeId, desc);
            _serviceTypeIdByContractType.Add(desc.ContractType, desc.TypeId);

            foreach (var operation in desc.Operations)
                _dispatcherByMsgId.Add(operation.RequestMessageId, operation.Dispatch);

            Log.Info("Registered Entity<contractType:{0}, typeId:{1}>", desc.ContractType, desc.TypeId);
        }

        public void DispatchOneWay(object service, Message msg)
        {
            Action<IMessageFactory, object, Message> dispatch;
            if (_dispatcherByMsgId.TryGetValue(msg.Id, out dispatch))
                dispatch(_messageFactory, service, msg);
        }

        public NodeServiceProxy CreateProxy(NodeServiceKey serviceKey, IMessageFactory messageFactory, INetClient client)
        {
            NodeServiceProxy proxy;
            NodeServiceContractDescription desc;
            if (_descByTypeId.TryGetValue(serviceKey.TypeId, out desc))
            {
                proxy = (NodeServiceProxy)FormatterServices.GetUninitializedObject(desc.ProxyType);
                proxy.Init(serviceKey, desc, messageFactory, client);
            }
            else
                throw new Exception("Unregistered entity typeId - " + serviceKey.TypeId);
            
            return proxy;
        }

        public NodeServiceKey GetKey<ServiceContract>(uint serviceId = 0)
        {
            uint typeId;
            if (_serviceTypeIdByContractType.TryGetValue(typeof(ServiceContract), out typeId))
                return new NodeServiceKey(typeId, serviceId);
            else
                throw new Exception("Unregistered service cotract - " + typeof(ServiceContract).FullName);
        }

        public uint GetContractId(Type contractType)
        {
            uint typeId;
            if (_serviceTypeIdByContractType.TryGetValue(contractType, out typeId))
                return typeId;
            else
                throw new Exception("Unregistered entity cotract - " + contractType.FullName);
        }

        public bool TryGetContractId(Type contractType, out uint typeId)
        {
            return _serviceTypeIdByContractType.TryGetValue(contractType, out typeId);
        }

        public Type GetContractType(uint serviceTypeId)
        {
            NodeServiceContractDescription desc;
            if (_descByTypeId.TryGetValue(serviceTypeId, out desc))
                return desc.ContractType;
            else
                return null;
        }

        public NodeServiceContractDescription GetDescription(uint typeId)
        {
            NodeServiceContractDescription desc;
            if (_descByTypeId.TryGetValue(typeId, out desc))
                return desc;
            else
                return null;
        }

    }

    public enum BasicErrorCode
    {
        DispatcherNotFound
    }
}
