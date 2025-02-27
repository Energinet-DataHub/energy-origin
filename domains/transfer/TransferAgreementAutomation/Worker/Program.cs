using System;
using DataContext;
using EnergyOrigin.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TransferAgreementAutomation.Worker;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Options;
using TransferAgreementAutomation.Worker.Service;
using TransferAgreementAutomation.Worker.Service.Engine;
using TransferAgreementAutomation.Worker.Service.TransactionStatus;
using EnergyOrigin.WalletClient;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddOptions<TransferAgreementAutomationOptions>().BindConfiguration(TransferAgreementAutomationOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "TransferAgreementAutomation.Worker"))
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddMeter(TransferAgreementAutomationMetrics.MetricName)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint))
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));

builder.Services.AddHealthChecks();
builder.Services.AddLogging();

builder.Services.AddDbContext<ApplicationDbContext>(
    (sp, options) => options.UseNpgsql(
        sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString(),
        providerOptions => providerOptions.EnableRetryOnFailure()
    ),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddHostedService<TransferAgreementsAutomationWorker>();
builder.Services.AddSingleton<ITransferAgreementAutomationMetrics, TransferAgreementAutomationMetrics>();

builder.Services.AddSingleton<TransferEngineUtility>();
builder.Services.AddSingleton<IRequestStatusRepository, InMemoryRequestStatusRepository>();
builder.Services.AddSingleton<ITransferEngine, TransferAllCertificatesEngine>();
builder.Services.AddSingleton<ITransferEngine, TransferCertificatesBasedOnConsumptionEngine>();
builder.Services.AddSingleton<ITransferEngineCoordinator, TransferEngineCoordinator>();

builder.Services.AddHttpClient<TransferAgreementsAutomationWorker>();
builder.Services.AddHttpClient<IWalletClient, WalletClient>((sp, c) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
    c.BaseAddress = new Uri(options.WalletUrl);
});

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
