using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using MOUSE.Core.Azure.ServiceFabric;
using Serilog.Core;
using Serilog.Events;

namespace MOUSE.Core.Logging.Serilog
{
    public class AzureServiceFabricSerilogEnricher : ILogEventEnricher
    {
        readonly List<LogEventProperty> _properties = new List<LogEventProperty>();

        public AzureServiceFabricSerilogEnricher(ServiceContext context)
        {
            _properties =
                ServiceFabricHelpers.GetEnvironmentProperties(context)
                    .Select(pair => new LogEventProperty(pair.Key, new ScalarValue(pair.Value)))
                    .ToList();
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var property in _properties)
            {
                logEvent.AddPropertyIfAbsent(property);
            }
        }
    }
}
