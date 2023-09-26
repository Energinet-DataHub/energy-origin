using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Helpers;
using API.Models;
using API.Options;
using API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
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

builder.Services.AddOptions<EnergiDataServiceOptions>().BindConfiguration(EnergiDataServiceOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddTransient(x => x.GetRequiredService<IOptions<EnergiDataServiceOptions>>().Value);

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

builder.Services.AddHttpClient<IEnergiDataService, EnergiDataService>();
builder.Services.AddHttpClient<IDataSyncService, DataSyncService>();
builder.Services.AddTransient<IEmissionsService, EmissionsService>();

var app = builder.Build();

app.UseSwagger(o => o.RouteTemplate = "api-docs/emissions/{documentName}/swagger.json");
if (builder.Environment.IsDevelopment())
{
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/emissions/v1/swagger.json", "API v1"));
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
