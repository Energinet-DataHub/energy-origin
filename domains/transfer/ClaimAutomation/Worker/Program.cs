using System;
using API.ClaimAutomation.Api.Repositories;
using ClaimAutomation.Worker;
using ClaimAutomation.Worker.Automation;
using ClaimAutomation.Worker.Metrics;
using ClaimAutomation.Worker.Options;
using DataContext;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using EnergyOrigin.WalletClient;
using Npgsql;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddOptions<ClaimAutomationOptions>().BindConfiguration(ClaimAutomationOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<ApplicationDbContext>(
    (sp, options) => options.UseNpgsql(
        sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString(),
        providerOptions => providerOptions.EnableRetryOnFailure()
    ),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());

builder.Services.AddLogging();

builder.Services.AddScoped<IClaimAutomationRepository, ClaimAutomationRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IShuffler, Shuffler>();
builder.Services.AddHostedService<ClaimWorker>();
builder.Services.AddSingleton<AutomationCache>();
builder.Services.AddSingleton<IClaimAutomationMetrics, ClaimAutomationMetrics>();
builder.Services.AddHttpClient<IWalletClient, EnergyOrigin.WalletClient.WalletClient>((sp, c) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
    c.BaseAddress = new Uri(options.WalletUrl);
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "ClaimAutomation.Worker"))
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddMeter(ClaimAutomationMetrics.MetricName)
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

var app = builder.Build();
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.Run();

namespace ClaimAutomation
{
    public partial class Program;
}
