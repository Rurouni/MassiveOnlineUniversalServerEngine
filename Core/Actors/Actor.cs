using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;

namespace MOUSE.Core.Actors
{
    public interface IActor
    {
        string Name { get; }
        OperationContext Context { get; set; }
        ActorDescription Description { get; }
    }

    public class Actor : IActor, IOperationExecutor
    {
        ActorDescription _description;
        protected Logger Log;
        public ServerFiber Fiber;
        protected IServerNode Node;
        private string _name;
        private uint _localId;
        private string _fullName;
        private Dictionary<uint, NetContractHandler> _handlers;

        public string Name
        {
            get { return _name; }
        }

        public uint LocalId { get { return _localId; } }

        public ActorDescription Description { get { return _description; } }

        /// <summary>
        /// Any async method using this should be aware that Context is not restored in continuations, so only LockType.Full garanties Context remains the same,
        ///  or you can save Context in stack variable(or clojure)
        /// </summary>
        public OperationContext Context { get;set; }

        public void Init(string name, uint localId, ActorDescription desc, IServerNode node)
        {
            _name = name;
            _localId = localId;
            _fullName = string.Format("Actor<Name:{0}, LocalId:{1}, Type:{2}>",name, localId, GetType().Name);
            _description = desc;
            Node = node;
            Log = LogManager.GetLogger(_fullName);
            Fiber = new ServerFiber();
            _handlers = new Dictionary<uint, NetContractHandler>(); 
            foreach (NetContractDescription implementedContract in desc.ImplementedContracts)
            {
                _handlers.Add(implementedContract.TypeId, new NetContractHandler(node.Dispatcher, implementedContract.TypeId, this));
            }
            OnCreated();
        }

        public virtual void OnCreated()
        {
        }

        private Task<Message> DispatchAndReturnAsync(OperationContext context)
        {
            try
            {
                //TODO: research how to make this context propagate on async continuations
                Context = context;
                return Node.Dispatcher.Dispatch(this, context.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw new Exception("Error on DispatchAndReturnAsync", ex);
            }
        }

        Task<Message> IOperationExecutor.ExecuteOperation(OperationContext context)
        {
            NetContractDescription contract = Node.Dispatcher.GetContractForMessage(context.Message.Id);
            LockType lockType = _handlers[contract.TypeId].GetLockTypeForOperation(context.Message);
            return Fiber.Call(() => DispatchAndReturnAsync(context), lockType);
        }

        void IOperationExecutor.ExecuteOneWayOperation(OperationContext context)
        {
            LockType lockType = _handlers[Node.Dispatcher.GetContractForMessage(context.Message.Id).TypeId].GetLockTypeForOperation(context.Message);
            Fiber.ProcessAsync(() => DispatchAndReturnAsync(context), lockType);
        }

        public override string ToString()
        {
            return _fullName;
        }
    }
}
