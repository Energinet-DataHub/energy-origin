using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using API.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .Enrich.WithSpan();

var databaseConfiguration = builder.Configuration.GetSection(DatabaseOptions.Prefix);
var databaseOptions = databaseConfiguration.Get<DatabaseOptions>()!;

loggerConfiguration = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton(new NpgsqlDataSourceBuilder($"Host={databaseOptions.Host}; Port={databaseOptions.Port}; Database={databaseOptions.Name}; Username={databaseOptions.User}; Password={databaseOptions.Password};"));

builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter());

builder.Services.AddHealthChecks();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SupportNonNullableReferenceTypes();
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "documentation.xml"));
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Transfer Agreements API"
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // TODO: Is the following needed in this form after the new Auth service release?
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
            ValidateAudience = false,
            // Validate life time disabled as the JWT token generated from the auth service wrongly names the claim for expiration
            ValidateLifetime = false,
            SignatureValidator = (token, _) => new JwtSecurityToken(token)
        };
    });

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseSwagger(o => o.RouteTemplate = "api-docs/transfer-agreements/{documentName}/swagger.json");
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/transfer-agreements/v1/swagger.json", "API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
