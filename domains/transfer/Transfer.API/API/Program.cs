using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using API.Cvr;
using API.Cvr.Api.Clients.Cvr;
using API.Events;
using API.Transfer;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Clients;
using API.UnitOfWork;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Exceptions.Middleware;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.Setup.OpenTelemetry;
using EnergyOrigin.Setup.Pdf;
using EnergyOrigin.Setup.RabbitMq;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--migrate"))
{
    builder.AddSerilogWithoutOutboxLogs();
    builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    var migrateApp = builder.Build();
    var connectionString = migrateApp.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString();
    var dbMigrator = new DbMigrator(connectionString, typeof(ApplicationDbContext).Assembly,
        migrateApp.Services.GetRequiredService<ILogger<DbMigrator>>());
    await dbMigrator.MigrateAsync();
    return;
}

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddMassTransitAndRabbitMq<ApplicationDbContext>(x =>
{
    x.AddConsumer<TransferOrganizationRemovedFromWhitelistEventHandler, TransferOrganizationRemovedFromWhitelistEventHandlerDefinition>();
    x.AddConsumer<ReportRequestCreatedEventHandler, ReportRequestCreatedEventHandlerDefinition>();
});

builder.Services.AddPdfOptions();

builder.Services.AddScoped<IBearerTokenService, WebContextBearerTokenService>();
builder.Services.AddHttpClient<IAuthorizationClient, AuthorizationClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Authorization:BaseUrl"]!);
    })
    .AddPolicyHandler(RetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

AsyncRetryPolicy<HttpResponseMessage> RetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}

builder.Services.AddOpenTelemetryMetricsAndTracing("Transfer.API", otlpOptions.ReceiverEndpoint);

builder.Services.Configure<JsonOptions>(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddDbContext<DbContext, ApplicationDbContext>(
    (sp, options) =>
    {
        options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString(),
            providerOptions => providerOptions.EnableRetryOnFailure()
        );
    },
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());

builder.Services.AddValidatorsFromAssembly(typeof(API.Program).Assembly);

builder.Services.AddActivityLog(options => options.ServiceName = "transfer");


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddLogging();
builder.Services.AddTransfer();
builder.Services.AddCvr();
builder.Services.AddVersioningToApi();

builder.Services.AddSwagger("transfer");
builder.Services.AddSwaggerGen();
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddB2C(b2COptions);

var app = builder.Build();

app.MapHealthChecks("/health");

app.AddSwagger("transfer");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseMiddleware<ExceptionHandlerMiddleware>();

var activityLogApiVersionSet = app.NewApiVersionSet("activitylog").Build();
app.UseActivityLogWithB2CSupport().WithApiVersionSet(activityLogApiVersionSet)
    .HasApiVersion(ApiVersions.Version1AsInt);

app.Run();

namespace API
{
    public partial class Program
    {
    }
}
