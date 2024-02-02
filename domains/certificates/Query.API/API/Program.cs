using API.ContractService;
using API.DataSyncSyncer;
using API.Query.API;
using API.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Formatting.Json;
using System.Linq;
using System.Text.Json.Serialization;
using API.Configurations;
using Asp.Versioning;
using DataContext;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

var log = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpOptions.ReceiverEndpoint.ToString();
        options.IncludedData = IncludedData.MessageTemplateRenderingsAttribute |
                               IncludedData.TraceIdField | IncludedData.SpanIdField;
    });

var console = builder.Environment.IsDevelopment()
    ? log.WriteTo.Console()
    : log.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(console.CreateLogger());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "Query.API"))
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint))
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);


builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddQueryApi();
builder.Services.AddContractService();
builder.Services.AddDataSyncSyncer();
builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new HeaderApiVersionReader("EO_API_VERSION");
    })
    .AddMvc()
    .AddApiExplorer();

var tokenValidationOptions = builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.AddTokenValidation(tokenValidationOptions);

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseSwagger(o => o.RouteTemplate = "api-docs/certificates/{documentName}/swagger.json");
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(
        options =>
        {
            foreach (var description in app.DescribeApiVersions().OrderByDescending(x => x.GroupName))
            {
                options.SwaggerEndpoint(
                    $"/api-docs/certificates/{description.GroupName}/swagger.json",
                    $"API v{description.GroupName}");
            }
        });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
