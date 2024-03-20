using Serilog;
using System;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Formatting.Json;

namespace EnergyOrigin.Setup;
public static class LoggerBuilder
{
    public static Serilog.ILogger BuildSerilogger(Uri otlpReceiverEndpoint)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpReceiverEndpoint.ToString();
                options.IncludedData = IncludedData.MessageTemplateRenderingsAttribute |
                                       IncludedData.TraceIdField | IncludedData.SpanIdField;
            });

        loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());

        return loggerConfiguration.CreateLogger();
    }
}
