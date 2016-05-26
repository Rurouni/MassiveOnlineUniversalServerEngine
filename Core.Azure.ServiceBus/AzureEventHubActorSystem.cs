using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MOUSE.Core.Actors;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Misc;

namespace MOUSE.Core.Azure
{
    public class AzureEventHubActorSystem<TActor> : IActorSystem<TActor> where TActor : IActor
    {
        readonly ushort _id;
        
        readonly string _hubConnectionString;
        readonly string _blobStorageConnectionString;
        readonly bool _shouldProcessEvents;
        readonly INetNodeConfig _config;
        readonly INetNode _serverNode;
        readonly IMessageSerializer _serializer;
        readonly IAzureEventHubActorSystemEvents _logger;
        readonly TimeSpan _checkpointEveryTimespan;
        readonly string _hubName;
        readonly string _consumerGroup;
        EventHubClient _eventHubClient;

        protected ConcurrentDictionary<Guid, PendingOperation> PendingOperationsByRequestId = new ConcurrentDictionary<Guid, PendingOperation>();
        readonly Func<TActor> _actorFactory;
        readonly ConcurrentDictionary<string, TActor> _actors = new ConcurrentDictionary<string, TActor>();

        public AzureEventHubActorSystem(ushort id, string hubName, string hubConnectionString, string blobStorageConnectionString, bool shouldProcessEvents,
            Func<TActor> actorFactory, INetNodeConfig config,
            INetNode serverNode, IMessageSerializer serializer, IAzureEventHubActorSystemEvents logger, TimeSpan checkpointEveryTimespan)
        {
            _id = id;
            _hubConnectionString = hubConnectionString;
            _blobStorageConnectionString = blobStorageConnectionString;
            _shouldProcessEvents = shouldProcessEvents;
            _actorFactory = actorFactory;
            _config = config;
            _serverNode = serverNode;
            _serializer = serializer;
            _logger = logger;
            _checkpointEveryTimespan = checkpointEveryTimespan;
            _hubName = hubName;
            _consumerGroup = typeof (TActor).Name;
        }

        public ushort Id
        {
            get { return _id; }
        }

        public void Start()
        {
            _serverNode.ChannelOpenedEvent.Subscribe(OnDirectReplyChannelEstablished);
            _eventHubClient = EventHubClient.CreateFromConnectionString(_hubConnectionString, _hubName);

            if (_shouldProcessEvents)
            {
                //NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(_hubConnectionString);
                //EventHubDescription ehd = namespaceManager.GetEventHub(_hubName);
                //namespaceManager.CreateConsumerGroupIfNotExists(ehd.Path, _consumerGroup);

                var host = new EventProcessorHost(_serverNode.InstanceName, _hubName, _consumerGroup, _hubConnectionString, _blobStorageConnectionString);

                IEventProcessorFactory processorFactory = new DelegatingEventProcessorFactory(_checkpointEveryTimespan, ProcessEventHubEvent, _logger);

                var options = new EventProcessorOptions();
                options.ExceptionReceived += (sender, eventArgs) => _logger.EventProcessorHostException(eventArgs.Action, eventArgs.Exception);

                host.RegisterEventProcessorFactoryAsync(processorFactory, options).Wait();
            }

            _logger.ActorSystemStarted(_id);
        }

        async Task ProcessEventHubEvent(EventData ev)
        {
            if ((DateTime.UtcNow - ev.EnqueuedTimeUtc).TotalDays > 1)
            {
                _logger.ReceivedExpiredMessage(ev.EnqueuedTimeUtc);
                return;
            }
            Message msg;
            if(_serializer.TryDeserialize(new ArraySegment<byte>(ev.GetBytes()), out msg))
            {
                var actorHeader = msg.GetHeader<ActorTargetHeader>();
                if (actorHeader != null)
                {
                    _logger.ReceivedMessageForActor(actorHeader.ActorKey, msg);

                    var actor = _actors.GetOrAdd(actorHeader.ActorKey.Id, SpawnActor);
                    var opHeader = msg.GetHeader<OperationHeader>();
                    var actorReplyHeader = msg.GetHeader<ActorDirectReplyHeader>();
                    if (opHeader != null && opHeader.Type == OperationType.Request && actorReplyHeader != null)
                    {
                        var operationContext = new OperationContext(null, msg, opHeader.ActivityId);
                        Message reply = null;
                        try
                        {
                                
                            reply = await actor.Process(operationContext);
                        }
                        catch (ProcessingException ex)
                        {
                            reply = new ErrorMessage(ex.ErrorCode, ex.Message);
                        }

                        if (reply != null)
                        {
                            if (actorReplyHeader.ActorRef.Location.Equals(_serverNode.Address))
                            {
                                OnDirectReplyMessage(new OperationContext(null, reply, opHeader.ActivityId));
                            }
                            else
                            {
                                try
                                {
                                    INetChannel replyChannel = await _serverNode.Connect(actorReplyHeader.ActorRef.Location);
                                    replyChannel.Reply(operationContext, reply);
                                }
                                catch (Exception ex)
                                {
                                    _logger.FailedToSendReply(actorHeader.ActorKey, msg, ex);
                                }
                            }
                        }
                    }
                    else
                    {
                        var activityId = opHeader?.ActivityId ?? Guid.NewGuid();
                        try
                        {
                            await actor.Process(new OperationContext(null, msg, activityId));
                        }
                        catch (ProcessingException ex)
                        {
                            //log and skip
                        }
                            
                    }
                }
                else
                    _logger.ReceivedMessageWithoutActorHeader(_id, msg);
            }
            else
                _logger.FailedToDeserializeEvent(_id, ev.PartitionKey, ev.SequenceNumber, ev.Offset);
        }

        TActor SpawnActor(string actorId)
        {
            var actor = _actorFactory();
            actor.Init(this, new ActorRef(_serverNode.Address, new ActorKey(actorId)));
            return actor;
        }

        void OnDirectReplyChannelEstablished(INetChannel channel)
        {
            channel.OperationReceivedEvent
                .Where(operation =>
                {
                    var header = operation.Message.GetHeader<OperationHeader>();
                    return header != null
                           && header.Type == OperationType.Reply
                           && PendingOperationsByRequestId.ContainsKey(header.RequestId);
                })
                .Subscribe(OnDirectReplyMessage);
        }

        void OnDirectReplyMessage(IOperationContext operation)
        {
            var msg = operation.Message;
            var header = operation.Message.GetHeader<OperationHeader>();

            PendingOperation continuation;
            if (PendingOperationsByRequestId.TryRemove(header.RequestId, out continuation))
            {
                continuation.Expiration.Cancel();
                var invMsg = msg as ErrorMessage;
                if (invMsg != null)
                    continuation.TCS.SetException(new ProcessingException(invMsg.ErrorCode, invMsg.ErrorString));
                else
                    continuation.TCS.SetResult(msg);
            }
            else
                _logger.ReceivedUnboundReply(_id, msg);
        }

        public Task<IActorProxy> GetActor(ActorKey actorKey)
        {
            var header = new ActorTargetHeader(actorKey);
            return Task.FromResult<IActorProxy>(
                new DelegatingActorProxy(new ActorRef(new IPEndPoint(IPAddress.None, 0), actorKey),
                    (context, msg) => SendMesageToActorAsync(header, new OperationContext(context.ReplyChannel, msg, context.ActivityId)),
                    (context, msg) => SendOneWayMesageToActor(header, new OperationContext(context.ReplyChannel, msg, context.ActivityId)),
                    Observable.Never<IActorProxy>()));
        }

        Task<Message> SendMesageToActorAsync(ActorTargetHeader actorTargetHeader, IOperationContext operation)
        {
            var msg = operation.Message;
            Guid requestId = Guid.NewGuid();

            var continuation = new PendingOperation(requestId);
            if (!PendingOperationsByRequestId.TryAdd(requestId, continuation))
                throw new Exception("RequestId has collided!!!");

            var expiration = Task.Delay(TimeSpan.FromSeconds(_config.SendTimeoutSec), continuation.Expiration.Token);
            expiration.ContinueWith(_=>
            {
                PendingOperation dummy;
                if (PendingOperationsByRequestId.TryRemove(continuation.RequestId, out dummy))
                {
                    continuation.TCS.SetException(
                        new TimeoutException($"SendMesageToActorAsync<{requestId}, {msg}> has Expired after {_config.SendTimeoutSec} s"));
                }
            });
            
            msg.AttachHeader(new ActorDirectReplyHeader(new ActorRef(_serverNode.Address, actorTargetHeader.ActorKey)));

            SendToEventHub(actorTargetHeader, msg, requestId, operation);

            return continuation.TCS.Task;
        }

        void SendOneWayMesageToActor(ActorTargetHeader actorTargetHeader, IOperationContext operation)
        {
            var msg = operation.Message;
            Guid requestId = Guid.NewGuid();

            SendToEventHub(actorTargetHeader, msg, requestId, operation);
        }

        async void SendToEventHub(ActorTargetHeader actorTargetHeader, Message msg, Guid requestId, IOperationContext operation)
        {
            msg.AttachHeader(new OperationHeader(requestId, OperationType.Request, operation.ActivityId));
            msg.AttachHeader(actorTargetHeader);
            using (var stream = new MemoryStream())
            {
                _serializer.Serialize(msg, stream);
                stream.Position = 0;
                try
                {
                    await _eventHubClient.SendAsync(new EventData(stream) { PartitionKey = actorTargetHeader.ActorKey.Id });
                }
                catch (Exception ex)
                {
                    _logger.FailedToSendMessage(requestId, msg, ex);
                }
            }
        }

        public Task<IActorProxy> GetActor(ActorRef actorRef)
        {
            throw new NotSupportedException();
        }

        public Task DisposeActor(ActorKey actorKey)
        {
            throw new NotImplementedException();
        }
    }
}
