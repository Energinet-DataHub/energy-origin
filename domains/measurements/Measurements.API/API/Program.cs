using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Helpers;
using API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Json;

[assembly: InternalsVisibleTo("Tests")]

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

builder.Services.AddHealthChecks().AddAsyncCheck("Configuration check", () =>
{
    try
    {
        Configuration.GetDataSyncEndpoint();
        return Task.FromResult(HealthCheckResult.Healthy());
    }
    catch
    {
        return Task.FromResult(HealthCheckResult.Unhealthy());
    }
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddHttpClient<IDataSyncService, DataSyncService>(client => client.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint()));
builder.Services.AddScoped<IMeasurementsService, MeasurementsService>();
builder.Services.AddScoped<IAggregator, MeasurementAggregation>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger(o => o.RouteTemplate = "api-docs/measurements/{documentName}/swagger.json");
if (builder.Environment.IsDevelopment())
{
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/measurements/v1/swagger.json", "API v1"));
}

app.UseAuthorization();
app.UseHttpLogging();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
