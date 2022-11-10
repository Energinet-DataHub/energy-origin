using System.IO;
using System.Reflection;
using API.GranularCertificateIssuer;
using API.MasterDataService;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Marten;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

builder.Services.AddHealthChecks();

builder.Services.AddMarten(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MartenDB");
    options.Connection(connectionString);

    options.AutoCreateSchemaObjects = AutoCreate.All;
});

builder.Services.AddMassTransit(o =>
{
    o.SetKebabCaseEndpointNameFormatter();

    // By default, sagas are in-memory, but should be changed to a durable
    // saga repository.
    //o.SetInMemorySagaRepositoryProvider();

    var entryAssembly = Assembly.GetEntryAssembly();

    o.AddConsumers(entryAssembly);
    //o.AddSagaStateMachines(entryAssembly);
    //o.AddSagas(entryAssembly);
    //o.AddActivities(entryAssembly);

    o.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});

builder.Services.AddSingleton<IEventStore, MemoryEventStore>();

builder.Services.AddMasterDataService(builder.Configuration);
//builder.Services.AddDataSyncSyncer();
builder.Services.AddGranularCertificateIssuer();

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
