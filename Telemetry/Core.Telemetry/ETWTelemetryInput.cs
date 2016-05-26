using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using EtwStream;
using Microsoft.Diagnostics.Tracing;
using Newtonsoft.Json;

namespace Core.Telemetry
{
    static public class ETWTelemetryInput
    {
        static public TelemetryPipe CollectETWEvents(this TelemetryPipe pipe, IEnumerable<ETWProvider> providers,  Dictionary<string, string> additionalProperties = null, bool isFilteringOnlyOwnProcess = true)
        {
            var pid = Process.GetCurrentProcess().Id;
            pipe.RegisterObserver(ObservableEventListener
                .FromTraceEvent(TraceEventLevel.Verbose, providers.Select(x=>x.Name).ToArray())
                .Where(ev => !isFilteringOnlyOwnProcess || ev.ProcessID == pid)
                .Select(ev => new TelemetryEvent()
                {
                    Type = "event",
                    Data = FromTraceEvent(ev, additionalProperties),
                    PublishDateTime = ev.TimeStamp
                }));
            
            return pipe;
        }

        static public TelemetryPipe CollectEventSourceEvents(this TelemetryPipe pipe, IEnumerable<ETWProvider> providers,  Dictionary<string, string> additionalProperties = null)
        {
            foreach (var provider in providers)
            {
                pipe.RegisterObserver(ObservableEventListener
                    .FromEventSource(provider.Name, provider.Level)
                    .Select(ev => new TelemetryEvent()
                    {
                        Type = "event",
                        Data = FromEventSourceEvent(ev, additionalProperties),
                        PublishDateTime = DateTime.UtcNow
                    }));
            }
            

            return pipe;
        }

        static ETWEvent FromTraceEvent(TraceEvent item, Dictionary<string,string> additionalProperties)
        {
            var data = new Dictionary<string, object>();

            for (var i = 0; i < item.PayloadNames.Length; i++)
                data.Add(item.PayloadNames[i], item.PayloadValue(i));

            if (additionalProperties != null)
            {
                foreach (var property in additionalProperties)
                {
                    if (!data.ContainsKey(property.Key))
                        data.Add(property.Key, property.Value);
                }
            }

            return new ETWEvent
            {
                Provider = item.ProviderName,
                EventName = item.EventName,
                EventId = (ushort)item.ID,
                Message = item.FormattedMessage ?? "none",
                Level = item.Level.ToString(),
                ActivityId = item.ActivityID,
                Timestamp = item.TimeStamp.ToUniversalTime(),
                Fields = data
            };
        }

        static ETWEvent FromEventSourceEvent(EventWrittenEventArgs item, Dictionary<string, string> additionalProperties)
        {
            var data = new Dictionary<string, object>();

            for (var i = 0; i < item.PayloadNames.Count; i++)
                data.Add(item.PayloadNames[i], item.Payload[i]);

            if (additionalProperties != null)
            {
                foreach (var property in additionalProperties)
                {
                    if(!data.ContainsKey(property.Key))
                        data.Add(property.Key, property.Value);
                }
            }

            return new ETWEvent
            {
                Provider = item.EventSource.Name,
                EventName = item.EventName,
                EventId = (ushort)item.EventId,
                Message = item.DumpFormattedMessage() ?? "none",
                Level = item.Level.ToString(),
                ActivityId = item.ActivityId,
                Timestamp = DateTime.UtcNow,
                Fields = data
            };
        }

    }

    public class ETWCollectorConfig
    {
        public ETWProvider[] Providers { get; set; }
    }

    public class ETWProvider
    {
        public string Name { get; set; }
        public EventLevel Level { get; set; }

        public ETWProvider(string name, EventLevel level = EventLevel.Verbose)
        {
            Name = name;
            Level = level;
        }
    }

    public class ETWEvent
    {
        public ushort EventId { get; set; }

        public string EventName { get; set; }

        public string Provider { get; set; }

        public Guid ActivityId { get; set; }
        public string Message { get; set; }

        public string Level { get; set; }

        [JsonProperty(PropertyName = "@timestamp")]
        public DateTime Timestamp { get; set; }

        public object Fields { get; set; }
    }
}