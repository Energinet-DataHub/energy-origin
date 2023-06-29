using System;
using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using ProjectOrigin.WalletSystem.V1;
using RegistryConnector.Worker;
using RegistryConnector.Worker.Cache;
using RegistryConnector.Worker.EventHandlers;
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

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.RabbitMq));
builder.Services.AddSingleton<ICertificateEventsInMemoryCache, CertificateEventsInMemoryCache>();
builder.Services.RegisterEventHandlers(builder.Configuration);

builder.Services.AddHostedService<NewClientWorker>();
builder.Services.AddGrpcClient<WalletService.WalletServiceClient>(o => o.Address = new Uri("http://localhost:7890"))
    .AddCallCredentials((context, metadata, sp) =>
        {
            metadata.Add("Authorization", $"Bearer {NewClientWorker.GenerateToken("issuer", "aud", Guid.NewGuid().ToString(), "foo")}");
            return Task.CompletedTask;
        }
    )
    .ConfigureChannel(o => o.UnsafeUseInsecureChannelCallCredentials = true);

builder.Services.AddGrpcClient<ReceiveSliceService.ReceiveSliceServiceClient>(o => o.Address = new Uri("http://localhost:7890"));

builder.Services.AddHealthChecks();

builder.Services.AddMassTransit(o =>
{
    o.SetKebabCaseEndpointNameFormatter();

    o.AddConsumer<ProductionCertificateCreatedEventHandler>();

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
    });
});

var app = builder.Build();

app.MapHealthChecks("/health");
app.SetupRegistryEvents();

app.MapPrometheusScrapingEndpoint();

app.Run();

public partial class Program
{
}
