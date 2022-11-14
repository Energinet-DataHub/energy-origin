using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using API.DataSyncSyncer;
using API.GranularCertificateIssuer;
using API.MasterDataService;
using API.QueryModelUpdater;
using API.RegistryConnector;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Marten;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Json;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .Filter
    .ByExcluding("RequestPath like '/health%'");

loggerConfiguration = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

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

builder.Services.AddMarten(provider =>
{
    var logger = provider.GetRequiredService<ILogger<Program>>();
    var connectionString = builder.Configuration.GetConnectionString("Marten");

    logger.LogInformation("ConnectionString: {connectionString}", connectionString);

    var store = new StoreOptions();
    store.Connection(connectionString);
    store.AutoCreateSchemaObjects = AutoCreate.All;
    return store;
}).ApplyAllDatabaseChangesOnStartup();

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IEventStore, MemoryEventStore>();

builder.Services.AddMasterDataService(builder.Configuration);
builder.Services.AddDataSyncSyncer();
builder.Services.AddGranularCertificateIssuer();
builder.Services.AddRegistryConnector();
builder.Services.AddQueryModelUpdater();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware to change authentication schema from "Bearer:" to "Bearer"
// This is a hack/work-around to handle how the auth service sets the Authorization header
app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        var authorizationHeader = context.Request.Headers.Authorization;
        var cleanedValues = authorizationHeader
            .Select(s => s.Replace("Bearer: ", "Bearer ", StringComparison.CurrentCultureIgnoreCase))
            .ToArray();

        context.Request.Headers.Authorization = new StringValues(cleanedValues);
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
