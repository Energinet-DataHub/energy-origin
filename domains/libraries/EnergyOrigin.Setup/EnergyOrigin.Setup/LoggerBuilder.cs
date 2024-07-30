using Serilog;
using Serilog.Formatting.Json;

namespace EnergyOrigin.Setup;

public static class LoggerBuilder
{
    public static Serilog.ILogger BuildSerilogger()
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'");

        loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());

        return loggerConfiguration.CreateLogger();
    }
}
