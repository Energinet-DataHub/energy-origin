using System;
using API.ContractService;
using API.Query.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using API.Configurations;
using API.MeasurementsSyncer;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.b2c;
using API.IssuingContractCleanup;
using API.MeasurementsSyncer.Metrics;
using API.Query.API.Controllers;
using API.UnitOfWork;
using Contracts;
using EnergyOrigin.Setup;
using MassTransit;
using Microsoft.Extensions.Options;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilogWithoutOutboxLogs();

builder.Services.AddOpenTelemetryMetricsAndTracing("Certificates.API", otlpOptions.ReceiverEndpoint)
    .WithMetrics(metricsBuilder => metricsBuilder.AddMeter(MeasurementSyncMetrics.MetricName));

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
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
builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

builder.Services.AddActivityLog(options => options.ServiceName = "certificates");

builder.Services.AddQueryApi();
builder.Services.AddContractService();
builder.Services.AddMeasurementsSyncer();
builder.Services.AddIssuingContractCleanup();
builder.Services.AddVersioningToApi();

var tokenValidationOptions =
    builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix)
    .ValidateDataAnnotations().ValidateOnStart();
var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddB2CAndTokenValidation(b2COptions, tokenValidationOptions);


var app = builder.Build();

app.MapHealthChecks("/health");

app.AddSwagger("certificates");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var activityLogApiVersionSet = app.NewApiVersionSet("activitylog").Build();
app.UseActivityLog().WithApiVersionSet(activityLogApiVersionSet)
    .HasApiVersion(ApiVersions.Version20240423AsInt)
    .HasApiVersion(ApiVersions.Version20230101AsInt);
app.UseActivityLogWithB2CSupport().WithApiVersionSet(activityLogApiVersionSet)
    .HasApiVersion(ApiVersions.Version20240515AsInt);


app.Run();

public partial class Program
{
}
