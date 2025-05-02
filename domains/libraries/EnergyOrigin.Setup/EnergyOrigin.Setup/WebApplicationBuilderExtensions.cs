using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace EnergyOrigin.Setup;

public static class WebApplicationBuilderExtensions
{
    public static void AddSerilog(this WebApplicationBuilder builder)
    {
        LoggerConfiguration log = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'");

        var console = builder.Environment.IsDevelopment()
            ? log.WriteTo.Console()
            : log.WriteTo.Console(new JsonFormatter());

        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(console.CreateLogger());
    }

    public static void AddSerilogWithoutOutboxLogs(this WebApplicationBuilder builder)
    {
        var log = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning);

        var console = builder.Environment.IsDevelopment()
            ? log.WriteTo.Console()
            : log.WriteTo.Console(new JsonFormatter());
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(console.CreateLogger());
    }
}
