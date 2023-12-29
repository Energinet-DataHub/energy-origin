using System;
using Contracts;
using DataContext;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using ProjectOrigin.Registry.V1;
using RegistryConnector.Worker;
using RegistryConnector.Worker.Converters;
using RegistryConnector.Worker.EventHandlers;
using RegistryConnector.Worker.RoutingSlips;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter());

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.RabbitMq));
builder.Services.AddOptions<RetryOptions>().BindConfiguration(RetryOptions.Retry).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddProjectOriginOptions();

builder.Services.AddScoped<IKeyGenerator, KeyGenerator>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!);

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

        cfg.ConfigureJsonSerializerOptions(jsonOptions =>
        {
            jsonOptions.Converters.Add(new TransactionConverter());
            jsonOptions.Converters.Add(new ReceiveRequestConverter());
            return jsonOptions;
        });
    });

    o.AddEntityFrameworkOutbox<ApplicationDbContext>(outboxConfigurator =>
    {
        outboxConfigurator.UsePostgres();

        outboxConfigurator.UseBusOutbox();
    });
});

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapPrometheusScrapingEndpoint();

app.Run();

public partial class Program;
