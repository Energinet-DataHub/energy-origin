using API.Authorization;
using API.Configuration;
using API.RabbitMq;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilogWithOpenTelemetry(otlpOptions.ReceiverEndpoint);

builder.AddOpenTelemetryMetricsAndTracing("Authorization.API", otlpOptions.ReceiverEndpoint);

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

// builder.Services.AddDbContext<DbContext, TransferDbContext>(
//     options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),
//     optionsLifetime: ServiceLifetime.Singleton);
// builder.Services.AddDbContextFactory<TransferDbContext>();
// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//builder.Services.AddHealthChecks().AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

//builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddAuthorizationApi();
//builder.Services.AddVersioningToApi();

var app = builder.Build();

//app.MapHealthChecks("/health");

//app.AddSwagger("authorization");

//app.UseHttpsRedirection();

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();

app.Run();

namespace API
{
    public partial class Program
    {
    }
}
