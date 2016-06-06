using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.Misc;
using Timer = Metrics.Timer;

namespace MOUSE.Core
{
    public class NetChannel : INetChannel
    {
        readonly Meter _receivedMessagesMeter;
        readonly Meter _sentMessagesMeter;
        readonly Meter _requestsMeter;
        readonly Meter _requestsFailedMeter;
        readonly Timer _requestDurationTimer;

        readonly INetNodeConfig _config;
        readonly IBufferPool _bufferPool;

        readonly Subject<INetChannel> _onDisconnectedSubject;
        readonly Subject<OperationContext> _onOperationSubject;

        public INetNode Node { get; }
        public ICoreEvents Logger { get; }
        public ITransportChannel TransportChannel { get; }
        public IMessageSerializer MessageSerializer { get; set; }
        
        protected ConcurrentDictionary<Guid, PendingOperation> PendingOperationsByRequestId;

        public IObservable<INetChannel> DisconnectedEvent => _onDisconnectedSubject;

        public IObservable<OperationContext> OperationReceivedEvent => _onOperationSubject;

        public bool IsConnected { get; private set; }

        public NetChannel(INetNode node, ITransportChannel transportChannel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
        {
            _onOperationSubject = new Subject<OperationContext>();
            _onDisconnectedSubject = new Subject<INetChannel>();
            PendingOperationsByRequestId = new ConcurrentDictionary<Guid, PendingOperation>();

            TransportChannel = transportChannel;
            var metricContext = Metric.Context(node.InstanceName);
            _receivedMessagesMeter = metricContext.Meter("ReceivedMessagesMeter", Unit.Requests);
            _sentMessagesMeter = metricContext.Meter("SentMessagesMeter", Unit.Requests);
            _requestsMeter = metricContext.Meter("SentRequestsMeter", Unit.Requests);
            _requestsFailedMeter = metricContext.Meter("SentRequestsFailedMeter", Unit.Requests);
            _requestDurationTimer = metricContext.Timer("SentRequestsDurationTimer", Unit.Requests, durationUnit: TimeUnit.Milliseconds);


            Node = node;
            Logger = logger;
            _config = config;
            _bufferPool = bufferPool;

            MessageSerializer = serializer;
            IsConnected = true;
            transportChannel.Bind(ProcessIncomingRequest, Dispose);
        }

        void ProcessIncomingRequest(ArraySegment<byte> data)
        {
            _receivedMessagesMeter.Mark();

            Message msg;
            if (MessageSerializer.TryDeserialize(data, out msg))
            {
                Logger.NetChannelHasReceivedMessage(Node, this, msg);
                var operationHeader = msg.GetHeader<OperationHeader>();
                if (operationHeader != null && operationHeader.Type == OperationType.Reply)
                {
                    PendingOperation continuation;
                    if (PendingOperationsByRequestId.TryRemove(operationHeader.RequestId, out continuation))
                    {
                        continuation.Expiration.Cancel();
                        if (!continuation.TCS.Task.IsCompleted)
                        {
                            var durationMs = (DateTime.Now - continuation.StartTime).Milliseconds;
                            _requestDurationTimer.Record(durationMs, TimeUnit.Milliseconds);
                            Logger.NetChannelRequestCompleted(Node, this, msg, operationHeader.RequestId, durationMs);

                            var invMsg = msg as ErrorMessage;
                            if (invMsg != null)
                            {
                                _requestsFailedMeter.Mark();
                                Logger.NetChannelRequestFailed(Node, this, invMsg, operationHeader.RequestId, durationMs);
                                continuation.TCS.SetException(new ProcessingException(invMsg.ErrorCode, invMsg.ErrorString));
                            }
                            else
                                continuation.TCS.SetResult(msg);
                        }
                    }
                    else
                        OnIncomingMessage(msg);
                }
                else
                {
                    OnIncomingMessage(msg);
                }
            }
        }

        protected virtual void OnIncomingMessage(Message msg)
        {
            _onOperationSubject.OnNext(new OperationContext(this, msg));
        }

        protected virtual void OnDisconnected() {}

        public Task<Message> SendRequestAsync(Message request)
        {
            _requestsMeter.Mark();

            //if operation header is already defined just use it eitherwise attach new one
            var opHeader = request.GetHeader<OperationHeader>();
            Guid requestId = Guid.NewGuid();
            if (opHeader == null)
            {
                request.AttachHeader(new OperationHeader(requestId, OperationType.Request, requestId));
            }
            else
            {
                requestId = opHeader.RequestId;
            }

            Logger.NetChannelRequestStarted(Node, this, request, requestId);

            var continuation = new PendingOperation(requestId);
            if (!PendingOperationsByRequestId.TryAdd(requestId, continuation))
                throw new Exception("RequestId GUID is not unique");

            var expiration = Task.Delay(TimeSpan.FromSeconds(_config.SendTimeoutSec), continuation.Expiration.Token);
            expiration.ContinueWith((_) =>
                {
                    try
                    {
                        PendingOperation dummy;
                        if (PendingOperationsByRequestId.TryRemove(continuation.RequestId, out dummy))
                        {
                            _requestsFailedMeter.Mark();
                            Logger.NetChannelRequestTimedOut(Node, this, request, requestId, _config.SendTimeoutSec * 1000);
                            if (!continuation.TCS.Task.IsCompleted)
                                continuation.TCS.SetException(new TimeoutException($"SendRequestAsync<{requestId}, {request}> has Expired after {_config.SendTimeoutSec} sec"));
                        }
                    }
                    catch {}
                });


            Send(request);

            return continuation.TCS.Task;
        }

        async public Task<TReply> SendRequestAsync<TReply>(Message request) 
            where TReply : Message
        {
            Message reply = await SendRequestAsync(request);
            return (TReply)reply;
        }

        public void Send(Message msg)
        {
            _sentMessagesMeter.Mark();
            var buffer = _bufferPool.Take(_config.MaxMessageSizeBts);
            try
            {
                using (var stream = new MemoryStream(buffer))
                {
                    MessageSerializer.Serialize(msg, stream);
                    TransportChannel.Send(msg.Priority, msg.Reliability, new ArraySegment<byte>(buffer, 0, (int)stream.Position));
                }

                Logger.NetChannelSentMessage(Node, this, msg);
            }
            finally
            {
                _bufferPool.Return(buffer);
            }
        }

        public void ReplyWithError(IOperationContext context, ushort errorCode, string errorDesc)
        {
            _requestsFailedMeter.Mark();
            var invalidMsg = new ErrorMessage { ErrorCode = errorCode, ErrorString = errorDesc };
            Logger.NetChannelSentReplyMessage(Node, this, invalidMsg, context);
            Reply(context, invalidMsg);
        }

        public void Reply(IOperationContext context, Message replyMsg)
        {
            if (context.ReplyRequired)
            {
                replyMsg.AttachHeader(new OperationHeader(context.RequestId.Value, OperationType.Reply, context.ActivityId));
                Logger.NetChannelSentReplyMessage(Node, this, replyMsg, context);
                Send(replyMsg);
            }
            else
            {
                Logger.CantReplyToOneWayMessage(Node, context);
            }
        }

        public IDisposable RegisterHandler<TMessage>(Action<TMessage> messageHandler)
            where TMessage : Message
        {
            return _onOperationSubject.Where(x=> x.Message is TMessage).Select(x => x.Message as TMessage).Subscribe(messageHandler);
        }

        public override string ToString()
        {
            return $"NetChannel<NetId:{TransportChannel.LocalId}, Endpoint:{TransportChannel.EndPoint}>";
        }
        
        public void Dispose()
        {
            if (IsConnected)
            {
                try
                {
                    IsConnected = false;
                    Logger.NetChannelIsDisposing(Node, this);

                    _onDisconnectedSubject.OnNext(this);
                    _onDisconnectedSubject.OnCompleted();
                    _onOperationSubject.OnCompleted();

                    OnDisconnected();

                    foreach (var pendingOperation in PendingOperationsByRequestId)
                    {
                        try
                        {
                            if (!pendingOperation.Value.TCS.Task.IsCompleted)
                                pendingOperation.Value.TCS.SetException(
                                    new OperationCanceledException("netChannel was closed"));
                        }
                        catch{}

                    }
                }
                finally 
                {
                    TransportChannel.Dispose();
                }
            }
        }
    }

    public class PendingOperation
    {
        public readonly TaskCompletionSource<Message> TCS;
        public readonly DateTime StartTime;
        public readonly Guid RequestId;
        public readonly CancellationTokenSource Expiration;

        public PendingOperation(Guid requestId)
        {
            TCS = new TaskCompletionSource<Message>();
            StartTime = DateTime.Now;
            RequestId = requestId;
            Expiration = new CancellationTokenSource();
        }
    }
}