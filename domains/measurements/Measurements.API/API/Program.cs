using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Options;
using API.Services;
using API.Shared.Swagger;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

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
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddLogging();

builder.Services.AddHttpClient<IDataSyncService, DataSyncService>(client => builder.Services.Configure<DataSyncOptions>(x => client.BaseAddress = x.Endpoint));
builder.Services.AddScoped<IMeasurementsService, MeasurementsService>();
builder.Services.AddScoped<IAggregator, MeasurementAggregation>();

builder.Services.AddOptions<DataHubFacadeOptions>().BindConfiguration(DataHubFacadeOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddGrpcClient<Meteringpoint.V1.Meteringpoint.MeteringpointClient>((sp, o) =>
{
    var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
    o.Address = new Uri(options.Url);
});
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

// Configure the HTTP request pipeline.
app.UseSwagger(o => o.RouteTemplate = "api-docs/measurements/{documentName}/swagger.json");
if (builder.Environment.IsDevelopment())
{
    app.UseSwaggerUI(
        options =>
        {
            foreach (var description in app.DescribeApiVersions().OrderByDescending(x => x.GroupName))
            {
                options.SwaggerEndpoint(
                    $"/api-docs/measurements/{description.GroupName}/swagger.json",
                    $"API v{description.GroupName}");
            }
        });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{

}
