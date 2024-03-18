using System;
using System.Linq;
using System.Text.Json.Serialization;
using Asp.Versioning;
using DataContext;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
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
using ProjectOrigin.WalletSystem.V1;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenTelemetry;
using Swashbuckle.AspNetCore.SwaggerGen;
using Transfer.Domain;
using TransferAgreementAutomation.Worker;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Options;
using TransferAgreementAutomation.Worker.Service;
using TransferAgreementAutomation.Worker.Swagger;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

var loggerConfiguration = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpOptions.ReceiverEndpoint.ToString();
        options.IncludedData = IncludedData.MessageTemplateRenderingsAttribute |
                               IncludedData.TraceIdField | IncludedData.SpanIdField;
    });

loggerConfiguration = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "TransferAgreementAutomation.ClaimAutomation"))
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

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

builder.Services.AddDbContext<ApplicationDbContext>(
    (sp, options) => options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString()),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddHostedService<TransferAgreementsAutomationWorker>();
builder.Services.AddSingleton<AutomationCache>();
builder.Services.AddSingleton<ITransferAgreementAutomationMetrics, TransferAgreementAutomationMetrics>();
builder.Services.AddSingleton<IProjectOriginWalletService, ProjectOriginWalletService>();

builder.Services.AddScoped<ISystemTime, SystemTime>();

builder.Services.AddHttpClient<TransferAgreementsAutomationWorker>();
builder.Services.AddGrpcClient<WalletService.WalletServiceClient>((sp, o) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
    o.Address = new Uri(options.WalletUrl);
});
builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new HeaderApiVersionReader("EO_API_VERSION");
    })
    .AddMvc()
    .AddApiExplorer();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

var tokenValidationOptions = builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.AddTokenValidation(tokenValidationOptions);

var app = builder.Build();

app.MapHealthChecks("/health");
app.UseSwagger(o => o.RouteTemplate = "api-docs/transfer-automation/{documentName}/swagger.json");
if (app.Environment.IsDevelopment())
    app.UseSwaggerUI(
        options =>
        {
            foreach (var description in app.DescribeApiVersions().OrderByDescending(x => x.GroupName))
            {
                options.SwaggerEndpoint(
                    $"/api-docs/transfer-automation/{description.GroupName}/swagger.json",
                    $"TransferAPI v{description.GroupName}");
            }
        });

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace TransferAgreementAutomation.Worker
{
    public partial class Program
    {
    }
}