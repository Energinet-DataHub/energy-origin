using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using API.DataSyncSyncer;
using API.GranularCertificateIssuer;
using API.MasterDataService;
using API.QueryModelUpdater;
using API.RegistryConnector;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .Filter
    .ByExcluding("RequestPath like '/health%'");

loggerConfiguration = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
var logger = loggerConfiguration.CreateLogger();
builder.Logging.AddSerilog(logger);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SupportNonNullableReferenceTypes();
    o.IncludeXmlComments(Path.Combine(System.AppContext.BaseDirectory, "documentation.xml"));
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Certificates Query API"
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IEventStore, MemoryEventStore>();

builder.Services.AddMasterDataService(builder.Configuration);
builder.Services.AddDataSyncSyncer();
builder.Services.AddGranularCertificateIssuer();
builder.Services.AddRegistryConnector();
builder.Services.AddQueryModelUpdater();

builder.Services.AddAuthentication(x =>
    {
        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
            ValidateAudience = false,
            SignatureValidator = (token, _) => new JwtSecurityToken(token)
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var headersAuthorization = context.Request.GetTypedHeaders().Headers.Authorization;
                logger.Information("OnMessageReceived - Token: {token}", context.Token);
                logger.Information("OnMessageReceived - Auth header: {requestAuth}", headersAuthorization);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var headersAuthorization = context.Request.GetTypedHeaders().Headers.Authorization;
                logger.Information("OnChallenge - header: {requestAuth}", headersAuthorization);
                if (headersAuthorization.Any(h => h.StartsWith("bearer", StringComparison.InvariantCultureIgnoreCase)))
                {
                    logger.Information("OnChallenge - HandleResponse");
                    context.HandleResponse();
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                logger.Information("OnAuthenticationFailed - Exception: {exception}", context.Exception);
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
