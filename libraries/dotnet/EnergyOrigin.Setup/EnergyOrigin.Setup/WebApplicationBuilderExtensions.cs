using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenTelemetry;

namespace EnergyOrigin.Setup;

public static class WebApplicationBuilderExtensions
{
    public static void AddSerilogWithOpenTelemetry(this WebApplicationBuilder builder)
    {
        var log = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'"));

        var console = builder.Environment.IsDevelopment()
            ? log.WriteTo.Console()
            : log.WriteTo.Console(new JsonFormatter());

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(console.CreateLogger());
    }

    public static void AddSerilogWithOpenTelemetryWithoutOutboxLogs(this WebApplicationBuilder builder)
    {
        var log = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning));

        var console = builder.Environment.IsDevelopment()
            ? log.WriteTo.Console()
            : log.WriteTo.Console(new JsonFormatter());

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(console.CreateLogger());
    }
}
