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
            throw new NotSupportedException("You should always override Configure in child class if you are using this class as base");
        }

        public void ReconfigureProcessingPipe(Func<IMessageProcessingPipeBuilder, IMessageProcessingPipeBuilder> reconfigurator)
        {
            _pipeBuilder = reconfigurator(_pipeBuilder);
            _processor = _pipeBuilder.Build();
        }

		//intended async void use
        protected async override void OnIncomingMessage(Message msg)
        {
			var operationContext = new OperationContext(this, msg);
			Guid prevActivityId = Trace.CorrelationManager.ActivityId;
			Trace.CorrelationManager.ActivityId = operationContext.ActivityId;
			try
			{
				var result = await _processor(operationContext);
				Logger.OperationProcessed(Node, this, operationContext);
				if (msg != null)
				{
					Reply(operationContext, result);
				}
			}
			catch (OperationCanceledException ocex)
			{
				Logger.OperationProcessingFailed(Node, this, operationContext, ocex);
				ReplyWithError(operationContext, (ushort)BasicErrorCode.OperationTimeout, "Operation timeout");
			}
			catch (Exception ex)
			{
				var baseException = ex.GetBaseException();
				Logger.OperationProcessingFailed(Node, this, operationContext, baseException);

				if (baseException is ProcessingException)
				{
					var invalidInputException = baseException as ProcessingException;
					ReplyWithError(operationContext, invalidInputException.ErrorCode, invalidInputException.Message);
				}
				else
					ReplyWithError(operationContext, (ushort)BasicErrorCode.Unknown, baseException.Message);
				throw;
			}
			finally
			{
				Trace.CorrelationManager.ActivityId = prevActivityId;
			}
		}

        protected override void OnDisconnected()
        {
            base.OnDisconnected();
            var operationContext = new OperationContext(this, new PeerDisconnected());
            _processor(operationContext); //ignore response message
        }
    }
}