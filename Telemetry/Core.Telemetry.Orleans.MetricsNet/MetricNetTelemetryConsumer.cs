using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Metrics;
using Metrics.Core;
using Orleans.Runtime;

namespace Core.Telemetry.Orleans.MetricsNet
{
	public class MetricNetTelemetryConsumer : IMetricTelemetryConsumer
	{
		readonly ConcurrentDictionary<string, double> _lastValues = new ConcurrentDictionary<string, double>();
		public void Flush(){}

		public void Close(){}

		public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
		{
			//hack because Metrics.Net supports only factory based Gauges
			_lastValues.AddOrUpdate(name,
				s =>
				{
					Metric.Gauge(name, () =>
					{
						double val;
						_lastValues.TryGetValue(name, out val);
						return val;
					}, Unit.None);

					return value;
				},
				(s, v) => value);

		}

		public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null)
		{
			Metric.Timer(name, Unit.None).Record((long)value.TotalMilliseconds, TimeUnit.Milliseconds);
		}

		public void IncrementMetric(string name)
		{
			Metric.Counter(name, Unit.None).Increment();
		}

		public void IncrementMetric(string name, double value)
		{
			Metric.Counter(name, Unit.None).Increment((long)value);
		}

		public void DecrementMetric(string name)
		{
			Metric.Counter(name, Unit.None).Decrement();
		}

		public void DecrementMetric(string name, double value)
		{
			Metric.Counter(name, Unit.None).Decrement((long)value);
		}
	}
}
