using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using ProjectOrigin.WalletSystem.V1;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;
using TransferAgreementAutomation.Worker;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Options;
using TransferAgreementAutomation.Worker.Service;


var builder = WebApplication.CreateBuilder(args);
var loggerConfiguration = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .Enrich.WithSpan();

loggerConfiguration = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<TransferApiOptions>().BindConfiguration(TransferApiOptions.TransferApi)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
    .ValidateDataAnnotations().ValidateOnStart();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(TransferAgreementAutomationMetrics.MetricName))
            .AddMeter(TransferAgreementAutomationMetrics.MetricName)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

builder.Services.AddHostedService<TransferAgreementsAutomationWorker>();
builder.Services.AddSingleton<AutomationCache>();
builder.Services.AddSingleton<ITransferAgreementAutomationMetrics, TransferAgreementAutomationMetrics>();
builder.Services.AddSingleton<IProjectOriginWalletService, ProjectOriginWalletService>();

builder.Services.AddHttpClient<TransferAgreementsAutomationWorker>();
builder.Services.AddGrpcClient<WalletService.WalletServiceClient>((sp, o) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
    o.Address = new Uri(options.WalletUrl);
});

var tokenValidationOptions = builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.AddTokenValidation(tokenValidationOptions);

var app = builder.Build();
app.MapHealthChecks("/health");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
