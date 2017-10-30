using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using MOUSE.Core.Actors;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Misc;

namespace MOUSE.Core.Azure.ServiceFabric
{
    public class ActorSystemNetChannel : NetChannel
    {
        public Action<OperationContext> ProcessIncommingMessage { get; set; }

        public ActorSystemNetChannel(INetNode node, ITransportChannel transportChannel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
            : base(node, transportChannel, serializer, logger, config, bufferPool)
        {}

        protected override void OnIncomingMessage(Message msg)
        {
            ProcessIncommingMessage?.Invoke(new OperationContext(this, msg));
        }
    }

    /// <summary>
    /// Actor system that runs on top of service fabric statefull service with custom transport
    /// if actorFactory is not provided will act as pure actor proxy provider but won't spawn any actors locally
    /// </summary>
    public class ServiceFabricActorSystemNetNode<TActor> : NetNode<ActorSystemNetChannel>, IActorSystem<TActor>
         where TActor : IActor
    {
        readonly IActorCoreEvents _logger;
        readonly Func<TActor> _actorFactory;
        readonly ConcurrentDictionary<ActorKey, TActor> _actors = new ConcurrentDictionary<ActorKey, TActor>();
        readonly Uri _serviceFabricServiceUri;
        readonly NetCommunicationClientFactory _communicationClientFactory;

        public ServiceFabricActorSystemNetNode(string name, Uri serviceFabricServiceUri, INetProvider net, IActorCoreEvents logger, ICoreEvents coreLogger, IMessageSerializer messageSerializer,
            INetNodeConfig config = null, IBufferPool bufferPool = null, IPEndPoint publicAddress = null, Func<TActor> actorFactory = null)
            : base(name, net, coreLogger, messageSerializer, null, config, publicAddress)
        {
            ChannelFactory = (node, transport) =>
            {
                var channel = new ActorSystemNetChannel(node, transport, messageSerializer, coreLogger, config, bufferPool);
                //don't process messages on local actors if factory is not provided
                if (actorFactory != null)
                {
                    channel.ProcessIncommingMessage = Process;
                }
                return channel;
            };

            _serviceFabricServiceUri = serviceFabricServiceUri;
            _actorFactory = actorFactory;
            _logger = logger;


            var servicePartitionResolver = new ServicePartitionResolver(() => new FabricClient());
            _communicationClientFactory = new NetCommunicationClientFactory(this, servicePartitionResolver);
        }

        async public void Process(IOperationContext operation)
        {
            Trace.CorrelationManager.ActivityId = operation.ActivityId;
            Stopwatch timer = Stopwatch.StartNew();
            var actorHeader = operation.Message.GetHeader<ActorTargetHeader>();
            if (actorHeader != null)
            {
                var actor = ActivateActor(actorHeader.ActorKey);

                _logger.ReceivedMessageForActor(actor, operation);
                try
                {
                    var reply = await actor.Process(operation);

                    _logger.ActorOperationSucceeded(actor, operation, timer.Elapsed.TotalMilliseconds);
                    if (reply != null)
                    {
                        operation.ReplyChannel.Reply(operation, reply);
                    }
                }
                catch (ProcessingException exception)
                {
                    _logger.ActorOperationFailed(actor, exception, operation, timer.Elapsed.TotalMilliseconds);
                    operation.ReplyChannel.ReplyWithError(operation, exception.ErrorCode, exception.Message);
                }
                catch (Exception exception)
                {
                    _logger.ActorOperationFailed(actor, exception, operation, timer.Elapsed.TotalMilliseconds);
#if DEBUG
                    operation.ReplyChannel.ReplyWithError(operation, (ushort)BasicErrorCode.Unknown, exception.ToString());
#else
                    operation.ReplyChannel.ReplyWithError(operation, (ushort)BasicErrorCode.Unknown, "Inernal Server Error");
#endif
                }
            }
            else
            {
                _logger.ReceivedNonActorMessage(operation);
                operation.ReplyChannel.ReplyWithError(operation, (ushort)BasicErrorCode.DispatcherFuncNotFound, "Actor Node can't process messages without actor header");
            }
        }


        public Task<IActorProxy> GetActor(ActorKey actorId)
        {
            var servicePartitionClient = new ServicePartitionClient<NetCommunicationClient>(_communicationClientFactory, _serviceFabricServiceUri,
                new ServicePartitionKey(actorId.Id.GetHashCode()), 
                targetReplicaSelector: TargetReplicaSelector.PrimaryReplica, 
                listenerName: InstanceName, 
                retrySettings: new OperationRetrySettings());

            return Task.FromResult<IActorProxy>(
                new DelegatingActorProxy(
                    new ActorRef(new IPEndPoint(IPAddress.None, 0), actorId),
                    async (context, msg) => //request - reply
                    {
                        msg.AttachHeader(new ActorTargetHeader(actorId));
                        return await servicePartitionClient.InvokeWithRetryAsync(
                            client =>
                            {
                                msg.AttachHeader(new OperationHeader(Guid.NewGuid(), OperationType.Request, context.ActivityId));
                                return client.Channel.SendRequestAsync(context.Message);
                            });
                    },
                    (context, msg) => //oneway
                    {
                        msg.AttachHeader(new ActorTargetHeader(actorId));
                        servicePartitionClient.InvokeWithRetry(
                            client =>
                            {
                                msg.AttachHeader(new OperationHeader(Guid.NewGuid(), OperationType.OneWay, context.ActivityId));
                                client.Channel.Send(context.Message);
                            });
                    },
                    Observable.Never<IActorProxy>()));
        }

        public Task<IActorProxy> GetActor(ActorRef actorRef)
        {
            throw new NotSupportedException("actor refs are not supported by service fabric actor system");
        }

        public Task DisposeActor(ActorKey actorKey)
        {
            TActor dummy;
            _actors.TryRemove(actorKey, out dummy);
            return TaskExt.Done;
        }

        TActor ActivateActor(ActorKey actorId)
        {
            return _actors.GetOrAdd(actorId, id =>
            {
                _logger.ActorActivated(this ,actorId);
                var actor = _actorFactory();
                actor.Init(this, new ActorRef(Address, id));
                return actor;
            });
        }

    }
}
