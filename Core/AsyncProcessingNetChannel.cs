using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Interfaces.MessageProcessing;
using MOUSE.Core.Interfaces.Serialization;
using MOUSE.Core.MessageProcessing;

namespace MOUSE.Core
{
    public class AsyncProcessingNetChannel : NetChannel
    {
        Func<IOperationContext, Task<Message>> _processor;
        IMessageProcessingPipeBuilder _pipeBuilder;

        public AsyncProcessingNetChannel(Func<IMessageProcessingPipeBuilder, IMessageProcessingPipeBuilder> configureProcesssingPipe, INetNode node,
            ITransportChannel transportChannel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
            : base(node, transportChannel, serializer, logger, config, bufferPool)
        {
            _pipeBuilder = configureProcesssingPipe(new MessageProcessingPipeBuilder());
            _processor = _pipeBuilder.Build();
        }

        public AsyncProcessingNetChannel(INetNode node, ITransportChannel transportChannel, IMessageSerializer serializer, ICoreEvents logger, INetNodeConfig config, IBufferPool bufferPool)
            : base(node, transportChannel, serializer, logger, config, bufferPool)
        {
            _pipeBuilder = Configure(new MessageProcessingPipeBuilder());
            _processor = _pipeBuilder.Build();
        }

        protected virtual IMessageProcessingPipeBuilder Configure(IMessageProcessingPipeBuilder builder)
        {
            throw new NotImplementedException("You should always override Configure in child class if you are using this class as base");
        }

        public void ReconfigureProcessingPipe(Func<IMessageProcessingPipeBuilder, IMessageProcessingPipeBuilder> reconfigurator)
        {
            _pipeBuilder = reconfigurator(_pipeBuilder);
            _processor = _pipeBuilder.Build();
        }

        protected override void OnIncomingMessage(Message msg)
        {
            var operationContext = new OperationContext(this, msg);
            Trace.CorrelationManager.ActivityId = operationContext.ActivityId;
            _processor(operationContext)
                .ContinueWith(msgTask => Reply(msgTask, operationContext));
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();
            var operationContext = new OperationContext(this, new PeerDisconnected());
            _processor(operationContext); //ignore response message
        }

        void Reply(Task<Message> msgTask, OperationContext operation)
        {
            if (msgTask.IsFaulted)
            {
                var baseException = msgTask.Exception.GetBaseException();
                Logger.OperationProcessingFailed(Node, this, operation, baseException);

                if (baseException is ProcessingException)
                {
                    var invalidInputException = baseException as ProcessingException;
                    ReplyWithError(operation, invalidInputException.ErrorCode, invalidInputException.Message);
                }
                else
                    ReplyWithError(operation, (ushort)BasicErrorCode.Unknown, baseException.Message);
            }
            else if (msgTask.IsCanceled)
            {
                Logger.OperationProcessingFailed(Node, this, operation, msgTask.Exception);
                ReplyWithError(operation, (ushort)BasicErrorCode.OperationTimeout, "Operation timeout");
            }
            else
            {
                Logger.OperationProcessed(Node, this, operation);
                var msg = msgTask.Result;
                if (msg != null)
                {
                    Reply(operation, msg);
                }
            }
        }
    }
}