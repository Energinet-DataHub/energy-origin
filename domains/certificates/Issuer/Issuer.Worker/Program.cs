using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Issuer.Worker.DataSyncSyncer;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.MasterDataService;
using Issuer.Worker.QueryModelUpdater;
using Issuer.Worker.RegistryConnector;
using Issuer.Worker.RegistryConnector.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddHealthChecks().AddCheck<HealthCheckWorker>("HealthCheckWorker" );
builder.Services.AddSingleton<IEventStore, MemoryEventStore>();
builder.Services.AddMasterDataService(builder.Configuration);
builder.Services.AddDataSyncSyncer();
builder.Services.AddGranularCertificateIssuer();
builder.Services.AddRegistryConnector();
builder.Services.AddQueryModelUpdater();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
