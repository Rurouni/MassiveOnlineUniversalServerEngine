using System.Net;
using LidgrenWrap;
using Serilog;
using Serilog.Events;

namespace MOUSE.Core.Logging.Serilog
{
    public class SerilogLidgrenEvents : ILidgrenEvents
    {
        readonly ILogger _logger;

        public SerilogLidgrenEvents(ILogger logger)
        {
            _logger = logger.ForContext<ILidgrenEvents>();
        }

        public void ConnectingTo(IPEndPoint target)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
                _logger.Verbose("Lidgren is connecting to {@address}", target);
        }

        public void Warning(string msg)
        {
            if (_logger.IsEnabled(LogEventLevel.Warning))
                _logger.Warning("Lidgren warning: {text}", msg);
        }

        public void Error(string msg)
        {
            if (_logger.IsEnabled(LogEventLevel.Error))
                _logger.Error("Lidgren error: {text}", msg);
        }

        public void Debug(string msg)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
                _logger.Verbose("Lidgren debug: {text}", msg);
        }

        public void OnDataReceived(long connectionId, int numBytes)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
                _logger.Verbose("Lidgren connection id:{connectionId} has received {numBytes} bytes", connectionId, numBytes);
        }

        public void StatusChanged(string status, long connectionId)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
                _logger.Verbose("Lidgren connection id:{connectionId} has changed status to {status}", connectionId, status);
        }
    }

    
}