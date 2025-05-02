using System.Linq;
using API.Configurations;
using API.ContractService;
using API.ContractService.EventHandlers;
using API.ContractService.Internal;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.Query.API;
using API.UnitOfWork;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Health;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.Setup.OpenTelemetry;
using EnergyOrigin.Setup.RabbitMq;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--migrate"))
{
    builder.AddSerilogWithoutOutboxLogs();
    var migrateApp = builder.Build();
    var dbMigrator = new DbMigrator(builder.Configuration.GetConnectionString("Postgres")!, typeof(ApplicationDbContext).Assembly,
        migrateApp.Services.GetRequiredService<ILogger<DbMigrator>>());
    await dbMigrator.MigrateAsync();
    return;
}

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilogWithoutOutboxLogs();

builder.Services.AddOpenTelemetryMetricsAndTracing("Certificates.API", otlpOptions.ReceiverEndpoint)
    .WithMetrics(metricsBuilder => metricsBuilder.AddMeter(MeasurementSyncMetrics.MetricName));

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<StampOptions>().BindConfiguration(StampOptions.Stamp).ValidateDataAnnotations()
    .ValidateOnStart();
// num nmu
builder.Services.AddDbContext<DbContext, ApplicationDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Postgres"),
            providerOptions => providerOptions.EnableRetryOnFailure()
        );
    },
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddMassTransitAndRabbitMq<ApplicationDbContext>(x =>
{
    x.AddConsumer<EnergyMeasuredIntegrationEventHandler, EnergyMeasuredIntegrationEventHandlerDefinition>();
    x.AddConsumer<ContractsOrganizationRemovedFromWhitelistEventHandler, ContractsOrganizationRemovedFromWhitelistEventHandlerDefinition>();
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddDefaultHealthChecks();

builder.Services.AddActivityLog(options => options.ServiceName = "certificates");
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<GetContractsForAdminPortalQueryHandler>();
    cfg.RegisterServicesFromAssemblyContaining<RemoveOrganizationContractsAndSlidingWindowsCommandHandler>();
});


builder.Services.AddQueryApi();
builder.Services.AddContractService();
builder.Services.AddMeasurementsSyncer();
//builder.Services.AddIssuingContractCleanup();
builder.Services.AddVersioningToApi();


var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddB2C(b2COptions);


var app = builder.Build();

app.MapDefaultHealthChecks();

app.AddSwagger("certificates");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var activityLogApiVersionSet = app.NewApiVersionSet("activitylog").Build();
app.UseActivityLogWithB2CSupport().WithApiVersionSet(activityLogApiVersionSet)
    .HasApiVersion(ApiVersions.Version1AsInt);

if (args.Contains("--swagger"))
{
    app.BuildSwaggerYamlFile(builder.Environment, "contracts.yaml");
}
else
{
    app.Run();
}

public partial class Program
{
}
