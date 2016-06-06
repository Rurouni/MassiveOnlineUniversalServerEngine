using System;
using System.Collections.Generic;
using System.Linq;
using Orleans.Runtime;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace Core.Telemetry.Orleans.Serilog
{
	public class SerilogTelemetryConsumer : ITraceTelemetryConsumer, IEventTelemetryConsumer, IExceptionTelemetryConsumer,
		IDependencyTelemetryConsumer,  IRequestTelemetryConsumer
	{
		private readonly ILogger _client;

		public SerilogTelemetryConsumer()
		{
			_client = Log.Logger;
		}

		public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration, bool success)
		{
			_client.Information("Dependency call to {dependencyName} with {commandName} success:{success} started at {startTime} duration:{durationInMs}ms",
				dependencyName, commandName, success, startTime, duration.TotalMilliseconds);
		}

		public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
		{
			if (_client.IsEnabled(LogEventLevel.Information))
			{
				var logProperties = new List<LogEventProperty>();
				if(properties!= null)
					logProperties.AddRange(properties.Select(x => new LogEventProperty(x.Key, new ScalarValue(x.Value))));

				if (metrics != null)
					logProperties.AddRange(metrics.Select(x => new LogEventProperty(x.Key, new ScalarValue(x.Value))));

				_client.Write(new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null,
					new MessageTemplate("{eventName}", new[] { new TextToken(eventName) }), logProperties));
			}
		}

		public void TrackException(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
		{
			if (_client.IsEnabled(LogEventLevel.Warning))
			{
				var logProperties = new List<LogEventProperty>();
				if (properties != null)
					logProperties.AddRange(properties.Select(x => new LogEventProperty(x.Key, new ScalarValue(x.Value))));

				if (metrics != null)
					logProperties.AddRange(metrics.Select(x => new LogEventProperty(x.Key, new ScalarValue(x.Value))));

				_client.Write(new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Warning, exception,
					new MessageTemplate("{exception}", new[] { new TextToken(exception.Message) }), logProperties));
			}
		}

		public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
		{
			_client.Information("Request:{name} processed as success:{success} responseCode:{responseCode} startedAt:{startTime}, duration:{durationInMs}ms", name, success, responseCode, startTime, duration );
		}

		public void TrackTrace(string message)
		{
			_client.Verbose(message);
		}

		public void TrackTrace(string message, IDictionary<string, string> properties)
		{
			TrackTrace(message, Severity.Verbose, properties);
		}

		public void TrackTrace(string message, Severity severity)
		{
			TrackTrace(message, severity, null);
		}

		public void TrackTrace(string message, Severity severity, IDictionary<string, string> properties)
		{
			LogEventLevel level = LogEventLevel.Verbose;
			switch (severity)
			{
				case Severity.Off:
					return;
				case Severity.Error:
					level = LogEventLevel.Error;
					break;
				case Severity.Warning:
					level = LogEventLevel.Warning;
					break;
				case Severity.Verbose:
				case Severity.Verbose2:
				case Severity.Verbose3:
					level = LogEventLevel.Verbose;
					break;
			}

			if (_client.IsEnabled(level))
			{
				var logProperties = new List<LogEventProperty>();
				if (properties != null)
					logProperties.AddRange(properties.Select(x => new LogEventProperty(x.Key, new ScalarValue(x.Value))));

				_client.Write(new LogEvent(DateTimeOffset.UtcNow, level, null,
					new MessageTemplate("{message}", new []{new TextToken(message)}), logProperties));
			}
		}

		public void Flush() { }
		public void Close() { }
	}
}