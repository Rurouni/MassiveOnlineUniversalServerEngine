using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Elasticsearch.Net;
using Metrics;
using MoreLinq;
using MOUSE.Core.Logging;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TimeUnit = Metrics.TimeUnit;

namespace Core.Telemetry
{
    public class ElasticSearchOutput
    {
        readonly Subject<TelemetryEvent> _internalStream = new Subject<TelemetryEvent>();

        readonly ElasticSearchOutputConfig _config;

        ActionBlock<IList<TelemetryEvent>> _uploadBlock;
        readonly ILog _logger;
        readonly ElasticClient _elasticClient;

        IDisposable _internalStreamBufferSubscription;

        readonly Meter _errorsMeter = Metric.Meter("ElasticSearchDrain_Errors", Unit.Errors);
        readonly Meter _eventsReceivedMeter = Metric.Meter("ElasticSearchDrain_EventsReceived", Unit.Events);
        readonly Counter _eventsInQueueCounter = Metric.Counter("ElasticSearchDrain_QueuedEvents", Unit.Events);
        readonly Meter _eventsSentMeter = Metric.Meter("ElasticSearchDrain_EventsSent", Unit.Events);
        readonly Timer _requestTimer = Metric.Timer("ElasticSearchDrain_ESRequestTimer", Unit.Events, rateUnit:TimeUnit.Seconds, durationUnit:TimeUnit.Seconds);
        readonly JsonSerializerSettings _jsonSerializerSettings;

        public ElasticSearchOutput(ElasticSearchOutputConfig config, PutIndexTemplateDescriptor indexTemplate = null)
        {
            _logger = LogProvider.For<ElasticSearchOutput>();

            _logger.Info("Creating");

            var connectionSettings = new ConnectionSettings(new Uri(config.ElasticSearchUri))
                .RequestTimeout(config.RetryInterval)
                .MaximumRetries(config.RetryCount);

            _elasticClient = new ElasticClient(connectionSettings);

            var batchCount = config.MaxBatchCount > 0 ? config.MaxBatchCount : 1;

            _uploadBlock = new ActionBlock<IList<TelemetryEvent>>(list => Send(list, throwExceptions: false), new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = config.BoundedCapacity,
                MaxDegreeOfParallelism = config.MaxDegreeOfParallelism,
            });

            _internalStreamBufferSubscription = _internalStream
                .Buffer(TimeSpan.FromSeconds(config.BufferTimeSec), batchCount)
                .Subscribe(_uploadBlock.AsObserver());

            _jsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

            _elasticClient.PutIndexTemplate(indexTemplate ?? new PutIndexTemplateDescriptor(config.ElasticSearchIndexName+"-template")
                .Template(config.ElasticSearchIndexName+"*")
                .Settings(x => x
                    .NumberOfReplicas(0)
                    .NumberOfShards(1))
                .Mappings(m => m
                    .Map("_default_", tm => tm
                        .AllField(af => af.Enabled(false))
                        .DynamicTemplates(d => d
                            .DynamicTemplate("all_strings_not_analyzed", dd => dd
                                .MatchMappingType("string")
                                .Match("*")
                                .Mapping(dm => dm
                                    .String(sm => sm
                                        .NotAnalyzed()))))
                        .Properties(p => p
                            .String(sp => sp
                                .Name("message")
                                .Index(FieldIndexOption.Analyzed))))));

            
            _config = config;
        }

        protected string BuildIndex(DateTime eventDateTime)
        {
            return $"{_config.ElasticSearchIndexName}-{eventDateTime.ToUniversalTime().ToString("yyyy.MM.dd")}";
        }

        protected void Send(IList<TelemetryEvent> events, bool throwExceptions = false)
        {
            if (events.Count == 0)
                return;

            try
            {
                if (events.Count > 1)
                {
                    var sb = new StringBuilder();
                    foreach (var telemetryEvent in events)
                    {
                        sb.AppendFormat("{{ \"index\" : {{ \"_index\":\"{0}\", \"_type\":\"{1}\" }} }}\n", BuildIndex(telemetryEvent.PublishDateTime), telemetryEvent.Type);
                        
                        sb.Append(JsonConvert.SerializeObject(telemetryEvent.Data, _jsonSerializerSettings) + "\n");
                    }

                    var bulkBody = sb.ToString();

                    using (_requestTimer.NewContext())
                    {
                        _elasticClient.LowLevel.Bulk<VoidResponse>(bulkBody);
                    }
                }
                else
                {
                    var doc = events[0];
                    var index = BuildIndex(doc.PublishDateTime);
                    var jsonPayload = JsonConvert.SerializeObject(doc.Data);

                    using (_requestTimer.NewContext())
                    {
                        _elasticClient.LowLevel.Index<VoidResponse>(index, doc.Type, jsonPayload);
                    }
                }

                _eventsInQueueCounter.Decrement(events.Count);
                _eventsSentMeter.Mark(events.Count);
            }
            catch (Exception ex)
            {
                _errorsMeter.Mark(events.Count);
                _eventsInQueueCounter.Decrement(events.Count);
                _logger.ErrorException("Failed to send events to elastic search", ex);
                if (throwExceptions)
                    throw;
            }
        }

        public void Process(TelemetryEvent item)
        {
            _eventsReceivedMeter.Mark();
            _eventsInQueueCounter.Increment();
            _internalStream.OnNext(item);
        }

        public void Process(IEnumerable<TelemetryEvent> items)
        {
            var telemetryEvents = items as List<TelemetryEvent> ?? items.ToList();
            _eventsReceivedMeter.Mark(telemetryEvents.Count);
            _eventsInQueueCounter.Increment(telemetryEvents.Count);
            var itemBatches = telemetryEvents.Batch(_config.MaxBatchCount);

            foreach (var batch in itemBatches)
            {
                var itemList = batch.ToList();


                Send(itemList, throwExceptions: false);
            }
        }
    }

    static public class ElasticSearchOutputHelper
    {
        static public TelemetryPipe SendToElasticSearch(this TelemetryPipe pipe, ElasticSearchOutputConfig config)
        {
            var esOutput = new ElasticSearchOutput(config);
            pipe.RegisterProcessor(esOutput.Process);
            return pipe;
        }
    }

    public class ElasticSearchOutputConfig
    {
        public string ElasticSearchUri { get; set; }
        public string ElasticSearchIndexName { get; set; }
        public int BufferTimeSec { get; set; }

        public int RetryCount { get; set; }
        public TimeSpan RetryInterval { get; set; }
        public int MaxBatchCount { get; set; }
        public int BoundedCapacity { get; set; }
        public int MaxDegreeOfParallelism { get; set; }

        public ElasticSearchOutputConfig()
        {
            BufferTimeSec = 5;
            RetryCount = 3;
            RetryInterval = TimeSpan.FromSeconds(3);
            MaxBatchCount = 400;
            BoundedCapacity = 1000000;
            MaxDegreeOfParallelism = 1;
        }
    }
}