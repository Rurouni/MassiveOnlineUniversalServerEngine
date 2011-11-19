using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace MOUSE.Core
{
    public interface IClientNode : IServiceNode
    {
        Task ConnectToServer(IPEndPoint serverEndPoint);
        void SetHandler<TNetContract>(TNetContract implementer);
        IObservable<IClientNode> DisconnectedEvent { get; }
    }

   
    /// <summary>
    /// uses internal Fiber to receive all continuations and process messages to achieve thread-safety and provide manual update loop(if needed)
    /// </summary>
    public class ClientNode : NetNode<NetPeer>, IClientNode
    {
        private Dictionary<uint, object> _handlersByNetContractId = new Dictionary<uint, object>();
        private Dictionary<ulong, NodeServiceProxy> _proxyCache = new Dictionary<ulong, NodeServiceProxy>();

        protected IPEndPoint ServerEndPoint;
        protected NetPeer ServerPeer;

        public Fiber Fiber;

        public ClientNode(INetProvider net, IMessageFactory msgFactory, IServiceProtocol protocol,
            bool manualUpdate = false, IPEndPoint serverEndpoint = null)
            : base(net, msgFactory, protocol, null, manualUpdate)
        {
            ServerEndPoint = serverEndpoint;
            if (SynchronizationContext.Current != null)
                Fiber = new Fiber(TaskScheduler.FromCurrentSynchronizationContext(), manualUpdate);
            else
                Fiber = new Fiber(manualUpdate);
        }

        protected override void OnNodeUpdate()
        {
            base.OnNodeUpdate();
            Fiber.ExecuteAllInplace();
        }

        public async Task ConnectToServer(IPEndPoint endPoint)
        {
            if (ServerPeer.Channel.EndPoint == endPoint)
                return;

            if (ServerPeer != null)
                ServerPeer.Channel.Close();

            ServerEndPoint = endPoint;
            ServerPeer = (NetPeer)await Connect(endPoint).ConfigureAwait(false);
            ServerPeer.Channel.Send(new ConnectToService(0, 0));//notify server that this is our master connection
        }

        public override NetPeer CreatePeer(INetChannel channel)
        {
            var peer = new NetPeer(channel, this);
            peer.MessageEvent.Subscribe((msg)=> Fiber.Process(() => OnMessage(msg)));
            return peer;
        }

        public IObservable<IClientNode> DisconnectedEvent
        {
            get
            {
                return from peer in PeerDisconnectedEvent
                       where peer.Channel.EndPoint == ServerEndPoint
                       select this;
            }
        }

        protected void OnMessage(Message msg)
        {
            var serviceHeader = msg.GetHeader<ServiceHeader>();
            var operationHeader = msg.GetHeader<OperationHeader>();
            if (serviceHeader != null && operationHeader.Type == OperationType.Request)
            {
                uint serviceContractId = Protocol.GetContractId(serviceHeader.TargetServiceId);
                object handler;
                if (_handlersByNetContractId.TryGetValue(serviceContractId, out handler))
                {
                    //NOTE: doesnt support server->client request-reply, only one way notifications
                    Protocol.Dispatch(handler, msg);
                }
            }
        }

        public async Task<Message> ExecuteServiceOperation(NodeServiceProxy proxy, Message input)
        {
            input.AttachHeader(new ServiceHeader(proxy.ServiceId));
            Message output = await proxy.Target.ExecuteOperation(input).ConfigureAwait(false);
            await Fiber.ContinueOn().ConfigureAwait(false);//we need this to support getting events only in Update func
            return output;
        }

        public async Task<TNetContract> GetService<TNetContract>(uint serviceLocalId = 0)
        {
            await Fiber.ContinueOn().ConfigureAwait(false);

            if (ServerPeer == null)
            {
                if (ServerEndPoint != null)
                    ServerPeer = (NetPeer)await base.Connect(ServerEndPoint).ConfigureAwait(false);
                else
                    throw new Exception("Client node must be explicitly connected or masterEndpoint specified in constructor");
            }

            ulong fullId = Protocol.GetFullId<TNetContract>(serviceLocalId);
            NodeServiceProxy proxy;
            if (!_proxyCache.TryGetValue(fullId, out proxy))
            {
                var reply = (ServiceAccessReply)await ServerPeer.ExecuteOperation(new ServiceAccessRequest(fullId)).ConfigureAwait(false);

                if (!reply.IsValid)
                    throw new Exception("Invalid Access");

                NetPeer serviceOwnerNode;

                if (reply.ServiceOwner != null)
                    serviceOwnerNode = (NetPeer)await base.Connect(reply.ServiceOwner.EndPoint).ConfigureAwait(false);
                else
                    serviceOwnerNode = ServerPeer;

                if (reply.AccessTicket > 0)
                    serviceOwnerNode.Channel.Send(new ConnectToService(fullId, reply.AccessTicket));

                proxy = Protocol.CreateProxy(fullId);
                proxy.Node = this;
                proxy.Target = serviceOwnerNode;
                await Fiber.ContinueOn().ConfigureAwait(false);
                _proxyCache.Add(fullId, proxy);
            }

            return (TNetContract)(object)proxy;
        }

        public void SetHandler<TNetContract>(TNetContract implementer)
        {
            Fiber.Process(() =>
            {
                _handlersByNetContractId[Protocol.GetContractId(typeof(TNetContract))] = implementer;
            });
        }
    }
}
