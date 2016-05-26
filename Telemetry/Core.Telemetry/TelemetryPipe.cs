using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Metrics;
using Metrics.MetricData;
using Metrics.Reporters;
using Metrics.Reports;
using MoreLinq;
using Newtonsoft.Json.Linq;

namespace Core.Telemetry
{
    public class TelemetryPipe : IDisposable
    {
        List<IObservable<TelemetryEvent>> _inputs = new List<IObservable<TelemetryEvent>>();
        List<Action<TelemetryEvent>> _outputs = new List<Action<TelemetryEvent>>();
        List<IDisposable> _subscriptions = new List<IDisposable>();

        public void RegisterObserver(IObservable<TelemetryEvent> observable)
        {
            _inputs.Add(observable);
        }

        public void RegisterProcessor(Action<TelemetryEvent> processor)
        {
            _outputs.Add(processor);
        }

        public IDisposable Start()
        {
            foreach (var input in _inputs)
                foreach (var output in _outputs)
                    _subscriptions.Add(input.Subscribe(output));

            return this;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }

    public class TelemetryEvent
    {
        public string Type { get; set; }
        public object Data { get; set; }
        public DateTime PublishDateTime { get; set; }
    }
}