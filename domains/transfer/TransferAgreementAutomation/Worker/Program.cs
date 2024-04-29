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
using ProjectOrigin.WalletSystem.V1;
using TransferAgreementAutomation.Worker;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Options;
using TransferAgreementAutomation.Worker.Service;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilogWithOpenTelemetry(otlpOptions.ReceiverEndpoint);

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
            .AddProcessInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint))
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddGrpcClientInstrumentation(grpcOptions =>
            {
                grpcOptions.SuppressDownstreamInstrumentation = true;
                grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                    activity.SetTag("requestVersion", httpRequestMessage.Version);
                grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                    activity.SetTag("responseVersion", httpResponseMessage.Version);
            })
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
builder.Services.AddSingleton<IProjectOriginWalletService, ProjectOriginWalletService>();

builder.Services.AddHttpClient<TransferAgreementsAutomationWorker>();
builder.Services.AddGrpcClient<WalletService.WalletServiceClient>((sp, o) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
    o.Address = new Uri(options.WalletUrl);
});

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
