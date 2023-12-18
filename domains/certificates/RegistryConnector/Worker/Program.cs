using System;
using Contracts;
using DataContext;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using ProjectOrigin.Registry.V1;
using RegistryConnector.Worker;
using RegistryConnector.Worker.EventHandlers;
using RegistryConnector.Worker.RoutingSlips;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var log = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .Enrich.WithSpan();

var console = builder.Environment.IsDevelopment()
    ? log.WriteTo.Console()
    : log.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(console.CreateLogger());

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

    o.AddConsumer<RegistryIssuer>();

    o.AddConsumer<WalletSliceSender>();

    o.AddConsumer<MeasurementEventHandler>();
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

        cfg.ConfigureJsonSerializerOptions(options =>
        {
            options.Converters.Add(new TransactionConverter());
            options.Converters.Add(new ReceiveRequestConverter());
            return options;
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

public partial class Program
{
}
