using System.Linq;
using API.Cvr;
using API.Shared.Options;
using API.Shared.Swagger;
using API.Transfer;
using Asp.Versioning;
using DataContext;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

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

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddDbContext<ApplicationDbContext>(
    (sp, options) => options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString()),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddHttpContextAccessor();

builder.Services.AddLogging();
builder.Services.AddTransfer();
builder.Services.AddCvr();
builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new HeaderApiVersionReader("EO_API_VERSION");
    })
    .AddMvc()
    .AddApiExplorer();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen( options => options.OperationFilter<SwaggerDefaultValues>() );

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;
builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));


var tokenValidationOptions = builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.AddTokenValidation(tokenValidationOptions);

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseSwagger(o => o.RouteTemplate = "api-docs/transfer/{documentName}/swagger.json");
if (app.Environment.IsDevelopment())
    app.UseSwaggerUI(
        options =>
        {
            foreach (var description in app.DescribeApiVersions().OrderByDescending(x => x.GroupName))
            {
                options.SwaggerEndpoint(
                    $"/api-docs/transfer/{description.GroupName}/swagger.json",
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
