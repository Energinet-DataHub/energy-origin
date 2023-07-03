using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using API.Data;
using API.Filters;
using API.Options;
using Audit.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

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

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString()));

Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .AuditTypeExplicitMapper(config => config
            .Map<TransferAgreement, TransferAgreementHistoryEntry>((evt, eventEntry, historyEntity) =>
            {
                if (eventEntry.Entity != null && eventEntry.Entity.GetType() == typeof(TransferAgreementHistoryEntry))
                {
                    return false;
                }

                var actorId = evt.CustomFields.ContainsKey("ActorId") ? evt.CustomFields["ActorId"].ToString() : null;
                var actorName = evt.CustomFields.ContainsKey("ActorName") ? evt.CustomFields["ActorName"].ToString() : null;

                historyEntity.Id = Guid.NewGuid();
                historyEntity.AuditDate = DateTimeOffset.UtcNow;
                historyEntity.AuditAction = eventEntry.Action;
                historyEntity.ActorId = actorId;
                historyEntity.ActorName = actorName;

                switch (eventEntry.Action)
                {
                    case "Insert":
                        historyEntity.TransferAgreementId = (Guid)eventEntry.ColumnValues["Id"];
                        break;
                    case "Update":
                    {
                        historyEntity.TransferAgreementId = (Guid)eventEntry.PrimaryKey.Values.First();
                        break;
                    }
                }
                return true;
            })));

builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter());

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());

builder.Services.AddControllers(options => options.Filters.Add<AuditDotNetFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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

    if (builder.Environment.IsDevelopment())
    {
        o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\""
        });
        o.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });
    }
});

builder.Services.AddScoped<ITransferAgreementRepository, TransferAgreementRepository>();
builder.Services.AddScoped<ITransferAgreementHistoryEntryRepository, TransferAgreementHistoryEntryRepository>();

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
if (app.Environment.IsDevelopment()) app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/transfer-agreements/v1/swagger.json", "API v1"));

app.UseHttpsRedirection();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
