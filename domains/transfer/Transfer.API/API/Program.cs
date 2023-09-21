using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using API.Clients.Cvr;
using API.Data;
using API.Filters;
using API.Metrics;
using API.Models;
using API.Options;
using API.Services;
using API.Services.ConnectionInvitationCleanup;
using API.TransferAgreementsAutomation;
using API.TransferAgreementsAutomation.Service;
using Audit.Core;
using FluentValidation;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Polly;
using ProjectOrigin.WalletSystem.V1;
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

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ConnectionInvitationCleanupServiceOptions>().BindConfiguration(ConnectionInvitationCleanupServiceOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<CvrOptions>().BindConfiguration(CvrOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString()));

builder.Services.AddSingleton<ITransferAgreementAutomationMetrics, TransferAgreementAutomationMetrics>();

Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .AuditTypeExplicitMapper(config => config
            .Map<TransferAgreement, TransferAgreementHistoryEntry>((evt, eventEntry, historyEntity) =>
            {
                var actorId = evt.CustomFields.ContainsKey("ActorId") ? evt.CustomFields["ActorId"].ToString() : null;
                var actorName = evt.CustomFields.ContainsKey("ActorName") ? evt.CustomFields["ActorName"].ToString() : null;

                historyEntity.Id = Guid.NewGuid();
                historyEntity.CreatedAt = DateTimeOffset.UtcNow;
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

builder.Services.AddHttpClient<CvrClient>((sp, c) =>
{
    var cvrOptions = sp.GetRequiredService<IOptions<CvrOptions>>().Value;
    c.BaseAddress = new Uri(cvrOptions.BaseUrl);
    c.SetBasicAuthentication(cvrOptions.User, cvrOptions.Password);
}).AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(new[]
{
    TimeSpan.FromSeconds(1),
    TimeSpan.FromSeconds(5)
}));

builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TransferAgreementAutomationMetrics.MetricName))
            .AddMeter(TransferAgreementAutomationMetrics.MetricName)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());

builder.Services.AddControllers(options => options.Filters.Add<AuditDotNetFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(o =>
{
    o.SupportNonNullableReferenceTypes();
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "documentation.xml"));
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Transfer API"
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

builder.Services.AddLogging();
builder.Services.AddScoped<ITransferAgreementRepository, TransferAgreementRepository>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IProjectOriginWalletService, ProjectOriginWalletService>();
builder.Services.AddScoped<ITransferAgreementHistoryEntryRepository, TransferAgreementHistoryEntryRepository>();
builder.Services.AddScoped<IConnectionInvitationRepository, ConnectionInvitationRepository>();
builder.Services.AddGrpcClient<WalletService.WalletServiceClient>(o => o.Address = new Uri(builder.Configuration["ProjectOrigin:WalletUrl"] ?? "http://localhost:8080"));
builder.Services.AddScoped<ITransferAgreementsAutomationService, TransferAgreementsAutomationService>();
builder.Services.AddHostedService<TransferAgreementsAutomationWorker>();
builder.Services.AddScoped<IConnectionInvitationCleanupService, ConnectionInvitationCleanupService>();
builder.Services.AddHostedService<ConnectionInvitationCleanupWorker>();
builder.Services.AddSingleton<StatusCache>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
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

app.UseSwagger(o => o.RouteTemplate = "api-docs/transfer/{documentName}/swagger.json");
if (app.Environment.IsDevelopment()) app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/transfer/v1/swagger.json", "API v1"));

app.UseHttpsRedirection();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
