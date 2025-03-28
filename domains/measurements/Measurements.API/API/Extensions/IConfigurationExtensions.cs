using EnergyOrigin.Setup;
using EnergyOrigin.Setup.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        builder.AddOpenTelemetryLogging();

        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app, builder.Environment);
        return app;
    }
}
