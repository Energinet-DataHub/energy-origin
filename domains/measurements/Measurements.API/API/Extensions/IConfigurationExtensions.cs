using API.Options;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using JsonFormatter = Serilog.Formatting.Json.JsonFormatter;

namespace API.Extensions;

public static class ConfigurationExtensions
{
    public static WebApplication BuildApp(this IConfigurationRoot configuration, string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(configuration, shouldDisposeConfiguration: true);

        builder.Services.AddOptions<OtlpOptions>()
            .BindConfiguration(OtlpOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app, builder.Environment);
        return app;
    }

    public static Serilog.ILogger GetSeriLogger(this IConfiguration configuration)
    {
        var otlpConfiguration = configuration.GetSection(OtlpOptions.Prefix);
        var otlpOptions = otlpConfiguration.Get<OtlpOptions>();

        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpOptions!.ReceiverEndpoint.ToString();
                options.IncludedData = IncludedData.MessageTemplateRenderingsAttribute |
                                       IncludedData.TraceIdField | IncludedData.SpanIdField;
            });

        loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());

        return loggerConfiguration.CreateLogger();
    }
}
