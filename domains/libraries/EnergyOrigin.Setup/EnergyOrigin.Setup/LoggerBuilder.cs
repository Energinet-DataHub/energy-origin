using Serilog;
using Serilog.Filters;
using Serilog.Formatting.Json;

namespace EnergyOrigin.Setup;

public static class LoggerBuilder
{
    public static Serilog.ILogger BuildSerilogger()
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient.OtlpMetricExporter"))
            .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient.OtlpTraceExporter"));

        loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());

        return loggerConfiguration.CreateLogger();
    }
}
