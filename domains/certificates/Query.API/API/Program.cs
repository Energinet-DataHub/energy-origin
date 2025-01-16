using System;
using System.Linq;
using API.Configurations;
using API.ContractService;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.Query.API;
using API.UnitOfWork;
using Contracts;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using RabbitMQ.Client;

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

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.RabbitMq));

builder.Services.AddDbContext<DbContext, ApplicationDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Postgres"),
            providerOptions => providerOptions.EnableRetryOnFailure()
        );
    },
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddMassTransit(
    x =>
    {
        x.AddConfigureEndpointsCallback((name, cfg) =>
        {
            if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                rmq.SetQuorumQueue(3);
        });

        x.AddConsumer<EnergyMeasuredIntegrationEventHandler, EnergyMeasuredIntegrationEventHandlerDefinition>();

        x.SetKebabCaseEndpointNameFormatter();

        x.UsingRabbitMq((context, cfg) =>
        {
            var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            var url = $"rabbitmq://{options.Host}:{options.Port}";

            cfg.Host(new Uri(url), h =>
            {
                h.Username(options.Username);
                h.Password(options.Password);
            });
            cfg.ConfigureEndpoints(context);
        });
        x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox();
        });
    }
);

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

    var factory = new ConnectionFactory
    {
        HostName = options.Host,
        Port = options.Port ?? 0,
        UserName = options.Username,
        Password = options.Password,
        AutomaticRecoveryEnabled = true
    };
    return factory.CreateConnection();
});

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!)
    .AddRabbitMQ();


builder.Services.AddActivityLog(options => options.ServiceName = "certificates");

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

app.MapHealthChecks("/health");

app.AddSwagger("certificates");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var activityLogApiVersionSet = app.NewApiVersionSet("activitylog").Build();
app.UseActivityLogWithB2CSupport().WithApiVersionSet(activityLogApiVersionSet)
    .HasApiVersion(ApiVersions.Version1AsInt)
    .HasApiVersion(ApiVersions.Version20240515AsInt);

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
