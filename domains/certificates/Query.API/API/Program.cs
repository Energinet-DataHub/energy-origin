using API.ContractService;
using API.Query.API;
using API.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using API.Configurations;
using API.MeasurementsSyncer;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using API.IssuingContractCleanup;
using EnergyOrigin.Setup;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilogWithOpenTelemetry(otlpOptions.ReceiverEndpoint);

builder.AddOpenTelemetryMetricsAndTracing("Certificates.API", otlpOptions.ReceiverEndpoint);

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<DbContext, ApplicationDbContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

builder.Services.AddActivityLog(options => options.ServiceName = "certificates");

builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddQueryApi();
builder.Services.AddContractService();
builder.Services.AddDataSyncSyncer();
builder.Services.AddIssuingContractCleanup();
builder.Services.AddVersioningToApi();

var tokenValidationOptions = builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.AddTokenValidation(tokenValidationOptions);

var app = builder.Build();

app.MapHealthChecks("/health");

app.AddSwagger("certificates");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseActivityLog();

app.Run();

public partial class Program
{
}
