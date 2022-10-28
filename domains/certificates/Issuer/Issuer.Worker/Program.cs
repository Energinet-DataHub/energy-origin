using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Issuer.Worker.DataSyncSyncer;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.MasterDataService;
using Issuer.Worker.QueryModelUpdater;
using Issuer.Worker.RegistryConnector;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .Filter
    .ByExcluding("RequestPath like '/health%'");

var logger = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger.CreateLogger());

builder.Services.AddSingleton<IEventStore, MemoryEventStore>();

builder.Services.AddMasterDataService(builder.Configuration);
builder.Services.AddDataSyncSyncer();
builder.Services.AddGranularCertificateIssuer();
builder.Services.AddRegistryConnector();
builder.Services.AddQueryModelUpdater();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
