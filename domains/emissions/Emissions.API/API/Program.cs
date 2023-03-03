using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Helpers;
using API.Models;
using API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Json;

[assembly: InternalsVisibleTo("Tests")]

var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddHealthChecks().AddAsyncCheck("Configuration check", () =>
{
    try
    {
        Configuration.GetDataSyncEndpoint();
        Configuration.GetEnergiDataServiceEndpoint();
        Configuration.GetRenewableSources();
        Configuration.GetWasteRenewableShare();
        return Task.FromResult(HealthCheckResult.Healthy());
    }
    catch
    {
        return Task.FromResult(HealthCheckResult.Unhealthy());
    }
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new CustomJsonStringEnumConverter<QuantityUnit>());
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddSingleton<IEmissionsCalculator, EmissionsCalculator>();
builder.Services.AddSingleton<ISourcesCalculator, SourcesCalculator>();

builder.Services.AddHttpClient<IEnergiDataService, EnergiDataService>(x => x.BaseAddress = new Uri(Configuration.GetEnergiDataServiceEndpoint()));
builder.Services.AddHttpClient<IDataSyncService, DataSyncService>(x => x.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint()));
builder.Services.AddTransient<IEmissionsService, EmissionsService>();

var app = builder.Build();

app.UseSwagger(o => o.RouteTemplate = "api-docs/emissions/{documentName}/swagger.json");
if (builder.Environment.IsDevelopment())
{
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/emissions/v1/swagger.json", "API v1"));
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
