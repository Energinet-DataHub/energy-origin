using System;
using Contracts;
using DataContext;
using EnergyOrigin.Setup;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.V1;
using RegistryConnector.Worker;
using RegistryConnector.Worker.Converters;
using RegistryConnector.Worker.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        providerOptions => providerOptions.EnableRetryOnFailure()
    ),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.RabbitMq));
builder.Services.AddOptions<RetryOptions>().BindConfiguration(RetryOptions.Retry).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddProjectOriginOptions();

builder.Services.AddScoped<IKeyGenerator, KeyGenerator>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

builder.Services.AddGrpcClient<RegistryService.RegistryServiceClient>((sp, o) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginRegistryOptions>>().Value;
    o.Address = new Uri(options.RegistryUrl);
});

builder.Services.AddMassTransit(o =>
{
    o.SetKebabCaseEndpointNameFormatter();

    o.AddConfigureEndpointsCallback((name, cfg) =>
    {
        if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
            rmq.SetQuorumQueue(3);
    });

    o.AddConsumer<MeasurementEventHandler, MeasurementEventHandlerDefinition>();
    o.AddConsumer<CertificateCreatedEventHandler, CertificateCreatedEventHandlerConsumerDefinition>();
    o.AddConsumer<CertificateFailedInRegistryEventHandler,
        CertificateFailedInRegistryEventHandlerConsumerDefinition>();
    o.AddConsumer<CertificateIssuedInRegistryEventHandler,
        CertificateIssuedInRegistryEventHandlerConsumerDefinition>();
    o.AddConsumer<CertificateMarkedAsIssuedEventHandler, CertificateMarkedAsIssuedEventHandlerConsumerDefinition>();
    o.AddConsumer<CertificateSentToRegistryEventHandler, CertificateSentToRegistryEventHandlerConsumerDefinition>();

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
            return jsonSerializerOptions;
        });
    });

    o.AddEntityFrameworkOutbox<ApplicationDbContext>(outboxConfigurator =>
    {
        outboxConfigurator.UsePostgres();
        outboxConfigurator.UseBusOutbox();
    });
});

builder.Services.AddOpenTelemetryMetricsAndTracingWithGrpcAndMassTransit("RegistryConnector",
    otlpOptions.ReceiverEndpoint);

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
