using System;
using Contracts;
using DataContext;
using MassTransit;
using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProjectOrigin.Registry.V1;
using RegistryConnector.Worker;
using RegistryConnector.Worker.Converters;
using RegistryConnector.Worker.EventHandlers;
using RegistryConnector.Worker.Metrics;
using RegistryConnector.Worker.RoutingSlips;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

var log = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpOptions.ReceiverEndpoint.ToString();
        options.IncludedData = IncludedData.MessageTemplateRenderingsAttribute |
                               IncludedData.TraceIdField | IncludedData.SpanIdField;
    });

var console = builder.Environment.IsDevelopment()
    ? log.WriteTo.Console()
    : log.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(console.CreateLogger());

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.RabbitMq));
builder.Services.AddOptions<RetryOptions>().BindConfiguration(RetryOptions.Retry).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddProjectOriginOptions();

builder.Services.AddScoped<IKeyGenerator, KeyGenerator>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

builder.Services.AddGrpcClient<RegistryService.RegistryServiceClient>((sp, o) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
    o.Address = new Uri(options.RegistryUrl);
});

builder.Services.AddMassTransit(o =>
{
    o.SetKebabCaseEndpointNameFormatter();

    o.AddConsumer<MeasurementEventHandler, MeasurementEventHandlerDefinition>();
    o.AddConsumer<IssueCertificateNotCompletedConsumer, IssueCertificateNotCompletedConsumerDefinition>();

    o.AddActivitiesFromNamespaceContaining<IssueToRegistryActivity>();

    o.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var url = $"rabbitmq://{options.Host}:{options.Port}";

        cfg.Host(new Uri(url), h =>
        {
            h.Username(options.Username);
            h.Password(options.Password);
        });

        cfg.ConfigureEndpoints(context);

        cfg.ConfigureJsonSerializerOptions(jsonSerializerOptions =>
        {
            jsonSerializerOptions.Converters.Add(new TransactionConverter());
            jsonSerializerOptions.Converters.Add(new ReceiveRequestConverter());
            return jsonSerializerOptions;
        });
    });

    o.AddEntityFrameworkOutbox<ApplicationDbContext>(outboxConfigurator =>
    {
        outboxConfigurator.UsePostgres();

        outboxConfigurator.UseBusOutbox();
    });
});

void ConfigureResource(ResourceBuilder r)
{
    r.AddService("RegistryConnector",
        serviceInstanceId: Environment.MachineName);
}


builder.Services.AddOpenTelemetry()
    .ConfigureResource(ConfigureResource)
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddMeter(IssuanceMetrics.Name)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter(InstrumentationOptions.MeterName)
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint))
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddSource(DiagnosticHeaders.DefaultListenerName)
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
