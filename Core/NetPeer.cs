using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MOUSE.Core
{
    public interface IOperationExecutor
    {
        Task<Message> ExecuteOperation(OperationContext context);
        void ExecuteOneWayOperation(OperationContext context);
    }

    public interface INetPeer : INetChannelListener, IOperationExecutor
    {
        void Init(INetChannel channel, INode node);

        INetChannel Channel { get; }

        IObservable<INetPeer> DisconnectedEvent { get; }
        IObservable<Message> MessageEvent { get; }

        IMessageFactory MessageFactory { get; }

        T As<T>();
    }

    /// <summary>
    /// All fields should be initialized only in Init
    /// </summary>
    public class NetPeer : INetPeer
    {
        private const int ExpirationTimeout = 30;

        public Logger Log;
        public INetChannel Channel { get; private set; }
        private Subject<INetPeer> _onDisconnectedSubject;
        private Subject<Message> _onMessageSubject;
        public INode Node { get; private set; }

        private int _requestId = 0;
        protected ConcurrentDictionary<int, PendingOperation> PendingOperationsByRequestId;
        private ConcurrentDictionary<uint, NetProxy> _proxyCacheByContractId;

        public virtual void Init(INetChannel channel, INode node)
        {
            Channel = channel;
            Node = node;
            Log = LogManager.GetLogger(ToString());
            _onMessageSubject = new Subject<Message>();
            _onDisconnectedSubject = new Subject<INetPeer>();
            PendingOperationsByRequestId = new ConcurrentDictionary<int, PendingOperation>();
            _proxyCacheByContractId = new ConcurrentDictionary<uint, NetProxy>();
        }

        void INetChannelListener.OnDisconnected()
        {
            _onDisconnectedSubject.OnNext(this);
            _onDisconnectedSubject.OnCompleted();
        }

        void INetChannelListener.OnNetData(BinaryReader reader)
        {
            Message msg = Node.MessageFactory.Deserialize(reader);
            var operationHeader = msg.GetHeader<OperationHeader>();
            if (operationHeader != null && operationHeader.Type == OperationType.Reply)
            {
                PendingOperation continuation;
                if (PendingOperationsByRequestId.TryRemove(operationHeader.RequestId, out continuation))
                {
                    continuation.Expiration.Cancel();
                    if (msg.Id == (uint)MessageId.InvalidOperation)
                    {
                        var invMsg = msg as InvalidOperation;
                        continuation.TCS.SetException(new InvalidInput(invMsg.ErrorCode, invMsg.DebugDescription));
                    }
                    else
                        continuation.TCS.SetResult(msg);
                }
                else
                    Log.Warn("Received Reply with requestId:{0}  with no continuation for it", operationHeader.RequestId);
            }
            else
            {
                _onMessageSubject.OnNext(msg);    
            }
            
        }

        public IObservable<T> ReceiveMessage<T>() where T : Message
        {
            return MessageEvent.OfType<T>();
        }

        public IObservable<T> ReceiveMessage<T>(TimeSpan waitTime) where T : Message
        {
            var cancelation = new CancellationTokenSource();
            cancelation.CancelAfter(waitTime);
            return MessageEvent.OfType<T>().RunAsync(cancelation.Token);
        }

        public IMessageFactory MessageFactory
        {
            get { return Node.MessageFactory; }
        }

        public virtual Task<Message> ExecuteOperation(OperationContext context)
        {
            int requestId = Interlocked.Increment(ref _requestId);
            context.Message.AttachHeader(new OperationHeader(requestId, OperationType.Request));
            
            var continuation = new PendingOperation(requestId);
            if (!PendingOperationsByRequestId.TryAdd(requestId, continuation))
                throw new Exception("This could happen only if requestId is duplicated");

            var expiration = Task.Delay(TimeSpan.FromSeconds(ExpirationTimeout), continuation.Expiration.Token);
            expiration.ContinueWith((_) =>
                {
                    PendingOperation dummy;
                    if (PendingOperationsByRequestId.TryRemove(continuation.RequestId, out dummy))
                        continuation.TCS.SetException(new Exception(string.Format("ExecuteOperation<{0}, {1}> has Expired after {2} sec", requestId, context.Message, ExpirationTimeout)));
                });

            Log.Debug("Sending " + context.Message);
            Channel.Send(context.Message);

            return continuation.TCS.Task;
        }

        public virtual T As<T>()
        {
            uint contractId = Node.Dispatcher.GetContractId(typeof(T));
            return (T)(object)_proxyCacheByContractId.GetOrAdd(contractId, createProxy);
        }

        private NetProxy createProxy(uint contractId)
        {
            return Node.Dispatcher.CreateProxy(contractId, MessageFactory, this);
        }

        public IObservable<INetPeer> DisconnectedEvent
        {
            get { return _onDisconnectedSubject; }
        }

        public IObservable<Message> MessageEvent
        {
            get { return _onMessageSubject; }
        }

        public override string ToString()
        {
            return string.Format("NodeProxy<NetId:{0}, Endpoint:{1}>", Channel.Id, Channel.EndPoint);
        }

        public void ExecuteOneWayOperation(OperationContext context)
        {
            Channel.Send(context.Message);
        }

        public void ReplyWithError(Message requestMsg, ushort errorCode, string errorDesc)
        {
            Reply(requestMsg, new InvalidOperation(errorCode, errorDesc));
        }

        public void Reply(Message requestMsg, Message replyMsg)
        {
            var opHeader = requestMsg.GetHeader<OperationHeader>();
            if (opHeader != null)
            {
                int requestId = opHeader.RequestId;
                replyMsg.AttachHeader(new OperationHeader(requestId, OperationType.Reply));
                Channel.Send(replyMsg);
            }
            else
            {
                Log.Warn("Can't reply to {0} with {1} as source operation is one way", requestMsg, replyMsg);
            }
        }
    }

    public class PendingOperation
    {
        public readonly TaskCompletionSource<Message> TCS;
        public readonly DateTime StartTime;
        public readonly int RequestId;
        public readonly CancellationTokenSource Expiration;

        public PendingOperation(int requestId)
        {
            TCS = new TaskCompletionSource<Message>();
            StartTime = DateTime.Now;
            RequestId = requestId;
            Expiration = new CancellationTokenSource();
        }
    }

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
}