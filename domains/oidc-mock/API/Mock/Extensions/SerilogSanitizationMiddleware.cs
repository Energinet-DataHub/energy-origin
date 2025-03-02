using System.Text.Encodings.Web;
using Serilog.Core;
using Serilog.Events;

namespace Oidc.Mock.Extensions;

public class SerilogSanitizationMiddleware
{
    public class SanitizeLogDestructuringPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object? value, ILogEventPropertyValueFactory propertyValueFactory,
            out LogEventPropertyValue result)
        {
            result = DestructureObject(value, propertyValueFactory);
            return true;
        }

        private LogEventPropertyValue DestructureObject(object? value,
            ILogEventPropertyValueFactory propertyValueFactory)
        {
            if (value is string str)
            {
                return new ScalarValue(HtmlEncoder.Default.Encode(str));
            }

            if (value == null || !value.GetType().IsClass)
            {
                return propertyValueFactory.CreatePropertyValue(value, destructureObjects: true);
            }

            var properties = value.GetType().GetProperties()
                .Where(prop => prop.CanRead)
                .Select(prop => new LogEventProperty(prop.Name,
                    DestructureObject(prop.GetValue(value, null), propertyValueFactory)))
                .ToList();

            return new StructureValue(properties);
        }
    }

    public class SanitizeLogEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var property in logEvent.Properties)
            {
                if (property.Value is not ScalarValue scalarValue ||
                    scalarValue.Value is not string stringValue) continue;
                var sanitizedValue = HtmlEncoder.Default.Encode(stringValue);
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, sanitizedValue));
            }
        }
    }
}
