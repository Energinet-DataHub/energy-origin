using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Options;
using API.Services;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Enrichers.Span;
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

builder.Services.AddHealthChecks();

builder.Services.AddOptions<DataSyncOptions>().BindConfiguration(DataSyncOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddTransient(x => x.GetRequiredService<IOptions<DataSyncOptions>>().Value);

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

builder.Services.AddLogging();

builder.Services.AddHttpClient<IDataSyncService, DataSyncService>(client => builder.Services.Configure<DataSyncOptions>(x => client.BaseAddress = x.Endpoint));
builder.Services.AddScoped<IMeasurementsService, MeasurementsService>();
builder.Services.AddScoped<IAggregator, MeasurementAggregation>();

var tokenValidationOptions = builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.AddTokenValidation(tokenValidationOptions);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger(o => o.RouteTemplate = "api-docs/measurements/{documentName}/swagger.json");
if (builder.Environment.IsDevelopment())
{
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/measurements/v1/swagger.json", "API v1"));
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
