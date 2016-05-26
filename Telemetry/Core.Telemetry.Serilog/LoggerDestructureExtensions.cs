using System;
using System.Diagnostics;
using System.Net;
using MOUSE.Core.Actors;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Actors;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace MOUSE.Core.Logging.Serilog
{
    static public class LoggerDestructureExtensions
    {
        static public LoggerConfiguration ConfigureMOUSETypesDestructure(this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration
                .Destructure.ByTransformingEx<INetNode>(x => new { x.InstanceName, x.Address })
                .Destructure.ByTransformingEx<IPEndPoint>(x => x.ToString())
                .Destructure.ByTransformingEx<INetChannel>(x => new { Id = x.TransportChannel.LocalId, Address = x.TransportChannel.EndPoint })
                .Destructure.ByTransformingEx<IActorProxy>(x => new {x.ActorRef.Key.Id, Address = x.ActorRef.Location })
                .Destructure.ByTransformingEx<IOperationContext>(x => new { RequestId = x.RequestId??Guid.Empty, x.ActivityId, x.Message })
                .Destructure.ByTransforming<ActorKey>(x => new {ActorId = x.Id});
        }

        static public LoggerConfiguration ByTransformingEx<TValue>(this LoggerDestructuringConfiguration loggerConfiguration, Func<TValue, object> transformation)
        {
            return loggerConfiguration.With(new ProjectedDestructuringPolicy(t => typeof(TValue).IsAssignableFrom(t), o => transformation((TValue)o)));
        }
    }

    class ProjectedDestructuringPolicy : IDestructuringPolicy
    {
        private readonly Func<Type, bool> _canApply;
        private readonly Func<object, object> _projection;

        public ProjectedDestructuringPolicy(Func<Type, bool> canApply, Func<object, object> projection)
        {
            if (canApply == null)
                throw new ArgumentNullException(nameof(canApply));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            this._canApply = canApply;
            this._projection = projection;
        }

        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (!this._canApply(value.GetType()))
            {
                result = (LogEventPropertyValue)null;
                return false;
            }
            object obj = this._projection(value);
            result = propertyValueFactory.CreatePropertyValue(obj, true);
            return true;
        }
    }

    public class ActivityIdSerilogEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty("ActivityId", new ScalarValue(Trace.CorrelationManager.ActivityId)));
        }
    }
}