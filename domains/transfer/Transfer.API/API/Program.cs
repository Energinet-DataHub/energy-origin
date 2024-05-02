using API.Cvr;
using API.Shared.Options;
using API.Transfer;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using API.UnitOfWork;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

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

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

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

var tokenValidationOptions =
    builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix)
    .ValidateDataAnnotations().ValidateOnStart();

builder.AddTokenValidation(tokenValidationOptions);

var app = builder.Build();

app.MapHealthChecks("/health");

app.AddSwagger("transfer");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseActivityLog();

app.Run();

public partial class Program
{
}
//test?
