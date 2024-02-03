using System;
using System.Linq;
using System.Text.Json.Serialization;
using Asp.Versioning;
using ClaimAutomation.Worker;
using ClaimAutomation.Worker.Api.Repositories;
using ClaimAutomation.Worker.Automation;
using ClaimAutomation.Worker.Automation.Services;
using ClaimAutomation.Worker.Metrics;
using ClaimAutomation.Worker.Options;
using ClaimAutomation.Worker.Swagger;
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

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddDbContext<ApplicationDbContext>(
    (sp, options) => options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString()),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

builder.Services.AddScoped<IClaimAutomationRepository, ClaimAutomationRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IProjectOriginWalletService, ProjectOriginWalletService>();
builder.Services.AddScoped<IShuffler, Shuffler>();
builder.Services.AddHostedService<ClaimWorker>();
builder.Services.AddSingleton<AutomationCache>();
builder.Services.AddSingleton<IClaimAutomationMetrics, ClaimAutomationMetrics>();
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

builder.AddTokenValidation(tokenValidationOptions);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "ClaimAutomation.Worker"))
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddMeter(ClaimAutomationMetrics.MetricName)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint))
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddGrpcClientInstrumentation(grpcOptions =>
            {
                grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                    activity.SetTag("requestVersion", httpRequestMessage.Version);
                grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                    activity.SetTag("responseVersion", httpResponseMessage.Version);
                grpcOptions.SuppressDownstreamInstrumentation = true;
            })
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));

var app = builder.Build();
app.MapHealthChecks("/health");
app.UseSwagger(o => o.RouteTemplate = "api-docs/claim-automation/{documentName}/swagger.json");
if (app.Environment.IsDevelopment())
    app.UseSwaggerUI(
        options =>
        {
            foreach (var description in app.DescribeApiVersions().OrderByDescending(x => x.GroupName))
            {
                options.SwaggerEndpoint(
                    $"/api-docs/claim-automation/{description.GroupName}/swagger.json",
                    $"API v{description.GroupName}");
            }
        });

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
