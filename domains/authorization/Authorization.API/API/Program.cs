using API.Authorization;
using API.Authorization.Exceptions;
using API.Configuration;
using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddOpenTelemetryMetricsAndTracing("Authorization.API", otlpOptions.ReceiverEndpoint);

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Audience = "f00b9b4d-3c59-4c40-b209-2ef87e509f54";
        options.MetadataAddress = "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0/.well-known/openid-configuration";
    });

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

var tokenValidationOptions =
    builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix)
    .ValidateDataAnnotations().ValidateOnStart();
var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddB2CAndTokenValidation(b2COptions, tokenValidationOptions);

// Register DbContext and related services
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Postgres"),
            providerOptions => providerOptions.EnableRetryOnFailure()
        );
    });
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// Register specific repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();

builder.Services.AddAuthorizationApi();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.AddSwagger("authorization");
app.MapHealthChecks("/health");

app.MapControllers();

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.Run();

namespace API
{
    public partial class Program
    {
    }
}
