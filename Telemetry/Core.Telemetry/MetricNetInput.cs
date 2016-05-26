using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Metrics;
using Metrics.MetricData;
using Metrics.Reporters;
using Metrics.Utils;
using MoreLinq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Core.Telemetry
{
    public class MetricNetInput
    {
        readonly ISubject<TelemetryEvent> _subject;

        public MetricNetInput(int reportingPeriodSec, Dictionary<string,string> additionalProperties, bool resetOnReport = false)
        {
            _subject = new Subject<TelemetryEvent>();

            Metric.Config.WithReporting(config => config.StopAndClearAllReports());

            Metric.Config.WithReporting(config =>
                config.WithReport(new RXMetricNetReport(_subject, additionalProperties, reportingPeriodSec, resetOnReport), TimeSpan.FromSeconds(reportingPeriodSec)));
        }
        
        public IObservable<TelemetryEvent> Stream => _subject;
    }

    static public class MetricNetInputHelper
    {
        static public TelemetryPipe CollectMetricsNet(this TelemetryPipe pipe, int reportingPeriodSec, Dictionary<string, string> additionalProperties = null, bool resetOnReport = false)
        {
            pipe.RegisterObserver(new MetricNetInput(reportingPeriodSec, additionalProperties, resetOnReport).Stream);

            return pipe;
        }
    }

    public class RXMetricNetReport : MetricsReport
    {
        const string EventType = "metric";
        readonly ISubject<TelemetryEvent> _subject;
        readonly Dictionary<string, string> _additionalProperties;
        readonly int _reportingPeriodSec;
        readonly bool _resetOnReport;

        public RXMetricNetReport(ISubject<TelemetryEvent> subject, Dictionary<string, string> additionalProperties, int reportingPeriodSec, bool resetOnReport = false)
        {
            _subject = subject;
            _additionalProperties = additionalProperties;
            _reportingPeriodSec = reportingPeriodSec;
            _resetOnReport = resetOnReport;
        }

        protected string FormatContextName(IEnumerable<string> contextStack, string contextName)
        {
            if (contextStack == null || !contextStack.Any())
                return "root";

            var stack = new List<string>(contextStack.Skip(1));

            if (stack.Count == 0)
                return contextName;

            stack.Add(contextName);
            
            return string.Join("_", stack);
        }

        protected string FormatMetricName<T>(string context, MetricValueSource<T> metric)
        {
            return $"{context}:{metric.Name}";
        }

        protected void ReportGauge(string name, double value, Unit unit, MetricTags tags)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return;

            var te = Pack("Gauge", name, unit, tags, new JObject{
                {"value", value}
            });

            _subject.OnNext(te);
        }

        protected void ReportCounter(string name, CounterValue value, Unit unit, MetricTags tags)
        {
            var payload = new JObject
            {
                {"count", value.Count}
            };
            value.Items.ForEach(i =>
            {
                payload.Add(i.Item + "Count", i.Count);
                payload.Add(i.Item + "Percent", i.Percent);
            });

            var te = Pack("Counter", name, unit, tags, payload);

            _subject.OnNext(te);
        }

        protected void ReportMeter(string name, MeterValue value, Unit unit, TimeUnit rateUnit, MetricTags tags)
        {
            var payload = new JObject
            {
                {"count", value.Count},
                {"meanRate", _resetOnReport ? ((double)value.Count / _reportingPeriodSec) : value.MeanRate},
                {"1MinRate", value.OneMinuteRate},
                {"5MinRate", value.FiveMinuteRate}
            };
            value.Items.ForEach(i =>
            {
                payload.Add(i.Item + "Count", i.Value.Count);
                payload.Add(i.Item + "Percent", i.Percent);
                payload.Add(i.Item + "MeanRate", i.Value.MeanRate);
                payload.Add(i.Item + "1MinRate", i.Value.OneMinuteRate);
                payload.Add(i.Item + "5MinRate", i.Value.FiveMinuteRate);
            });

            var te = Pack("Meter", name, unit, tags, payload);

            _subject.OnNext(te);
        }

        protected void ReportHistogram(string name, HistogramValue value, Unit unit, MetricTags tags)
        {
            var te = Pack("Histogram", name, unit, tags, new JObject {
                {"totalCount",value.Count},
                {"last", value.LastValue},
                {"min",value.Min},
                {"mean",value.Mean},
                {"max",value.Max},
                {"stdDev",value.StdDev},
                {"median",value.Median},
                {"percentile75",value.Percentile75},
                {"percentile95",value.Percentile95},
                {"percentile99",value.Percentile99},
                {"sampleSize", value.SampleSize}
            });

            _subject.OnNext(te);
        }

        protected void ReportTimer(string name, TimerValue value, Unit unit, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            var te = Pack("Timer", name, unit, tags, new JObject {
                {"totalCount",value.Rate.Count},
                {"activeSessions",value.ActiveSessions},
                {"meanRate", _resetOnReport ? ((double)value.Rate.Count / _reportingPeriodSec) : value.Rate.MeanRate},
                {"1MinRate", value.Rate.OneMinuteRate},
                {"5MinRate", value.Rate.FiveMinuteRate},
                {"last", value.Histogram.LastValue},
                {"min",value.Histogram.Min},
                {"mean",value.Histogram.Mean},
                {"max",value.Histogram.Max},
                {"stdDev",value.Histogram.StdDev},
                {"median",value.Histogram.Median},
                {"percentile75",value.Histogram.Percentile75},
                {"percentile95",value.Histogram.Percentile95},
                {"percentile99",value.Histogram.Percentile99},
                {"sampleSize", value.Histogram.SampleSize}
            });

            _subject.OnNext(te);
        }


        TelemetryEvent Pack(string type, string name, Unit unit, MetricTags tags, JObject payload)
        {
            if (_additionalProperties != null)
            {
                foreach (var property in _additionalProperties)
                    payload.Add(property.Key, property.Value);
            }
            var contextAndName = name.Split(':');
            payload.Add("group", contextAndName[0]);
            
            return new TelemetryEvent
            {
                Type = EventType,
                PublishDateTime = DateTime.UtcNow,
                Data = new SingleMetricSampleEvent
                {
                    Name = contextAndName[1],
                    Type = type,
                    Unit = unit.ToString(),
                    Tags = string.Join(" ", tags.Tags),
                    Timestamp = GetTimestampThatIsDivisableByPeriod(DateTime.UtcNow),
                    Fields = payload,
                }
            };
        }

        DateTime GetTimestampThatIsDivisableByPeriod(DateTime currentTimestamp)
        {
            return new DateTime(currentTimestamp.Ticks - currentTimestamp.Ticks % (_reportingPeriodSec * 10000000));
        }

        public void RunReport(MetricsData metricsData, Func<HealthStatus> healthStatus, CancellationToken token)
        {
            ReportContext(metricsData, Enumerable.Empty<string>());
        }

        void ReportContext(MetricsData data, IEnumerable<string> contextStack)
        {
            string contextName = FormatContextName(contextStack, data.Context);

            data.Gauges.ForEach(g => ReportGauge(FormatMetricName(contextName, g), g.ValueProvider.GetValue(false), g.Unit, g.Tags));
            data.Counters.ForEach(c => ReportCounter(FormatMetricName(contextName, c), c.ValueProvider.GetValue(false), c.Unit, c.Tags));

            data.Meters.ForEach(m => ReportMeter(FormatMetricName(contextName, m), m.ValueProvider.GetValue(_resetOnReport), m.Unit, m.RateUnit, m.Tags));
            data.Histograms.ForEach(h => ReportHistogram(FormatMetricName(contextName, h), h.ValueProvider.GetValue(_resetOnReport), h.Unit, h.Tags));
            data.Timers.ForEach(t => ReportTimer(FormatMetricName(contextName, t), t.ValueProvider.GetValue(_resetOnReport), t.Unit, t.RateUnit, t.DurationUnit, t.Tags));

            IEnumerable<string> newContextStack = contextStack.Concat(new[] { data.Context });
            foreach (MetricsData childMetric in data.ChildMetrics)
                ReportContext(childMetric, newContextStack);
        }
    }

    public class SingleMetricSampleEvent
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Unit { get; set; }

        public string Tags { get; set; }

        [JsonProperty(PropertyName = "@timestamp")]
        public DateTime Timestamp { get; set; }

        public object Fields { get; set; }
    }
}