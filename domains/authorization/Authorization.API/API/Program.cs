using System;
using System.Linq;
using API.Authorization;
using API.Authorization.EventHandlers;
using API.Data;
using API.Events;
using API.Metrics;
using API.Models;
using API.Options;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Exceptions.Middleware;
using EnergyOrigin.Setup.Health;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.Setup.OpenTelemetry;
using EnergyOrigin.Setup.RabbitMq;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.WalletClient;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--migrate"))
{
    builder.AddSerilogWithoutOutboxLogs();
    var migrateApp = builder.Build();
    var dbMigrator = new DbMigrator(builder.Configuration.GetConnectionString("Postgres")!, typeof(Program).Assembly,
        migrateApp.Services.GetRequiredService<ILogger<DbMigrator>>());
    await dbMigrator.MigrateAsync();
    return;
}

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddOpenTelemetryMetricsAndTracing("Authorization.API", otlpOptions.ReceiverEndpoint)
    .WithMetrics(metricsBuilder => metricsBuilder.AddMeter(AuthorizationMetrics.MetricName));

builder.AddSerilogWithoutOutboxLogs();

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddMassTransitAndRabbitMq<ApplicationDbContext>(cfg =>
{
    cfg.AddConsumer<AuthorizationOrganizationRemovedFromWhitelistEventHandler,
        AuthorizationOrganizationRemovedFromWhitelistEventHandlerDefinition>();
});

builder.Services.AddDefaultHealthChecks();

builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddB2C(b2COptions);

builder.Services.AddAuthentication(OpenApiConstants.Bearer)
    .AddMicrosoftIdentityWebApp(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraphAppOnly(autenticationProvider => new GraphServiceClient(autenticationProvider))
    .AddInMemoryTokenCaches();

builder.Services.AddHttpContextAccessor();

// Register DbContext and related services
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Postgres"),
            _ => { }
        );
    });

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Register specific repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IOrganizationConsentRepository, OrganizationOrganizationConsentRepository>();
builder.Services.AddScoped<ITermsRepository, TermsRepository>();
builder.Services.AddScoped<IWhitelistedRepository, WhitelistedRepository>();

// Metrics
builder.Services.AddSingleton<IAuthorizationMetrics, AuthorizationMetrics>();

builder.Services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
    .ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddHttpClient<IWalletClient, EnergyOrigin.WalletClient.WalletClient>((sp, c) =>
{
    var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
    c.BaseAddress = new Uri(options.WalletUrl);
});

builder.Services.AddAuthorizationApi();
builder.Services.AddOptions<MitIDOptions>().BindConfiguration(MitIDOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IMitIDService, MitIDService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MitIDOptions>>().Value;
    client.BaseAddress = options.URI;
});

builder.Services.AddScoped<ICredentialService, CredentialService>();
builder.Services.AddScoped<IGraphServiceClientWrapper, GraphServiceClientWrapper>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.AddSwagger("authorization");
app.MapHealthChecks("/health");

app.MapControllers();

app.UseMiddleware<ExceptionHandlerMiddleware>();

if (args.Contains("--swagger"))
{
    app.BuildSwaggerYamlFile(builder.Environment, "authorization.yaml", ApiVersions.Version1);
}
else
{
    app.Run();
}

namespace API
{
    public partial class Program
    {
    }
}
