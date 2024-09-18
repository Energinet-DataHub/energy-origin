using System;
using API.Authorization;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Options;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddOpenTelemetryMetricsAndTracing("Authorization.API", otlpOptions.ReceiverEndpoint);

builder.AddSerilogWithoutOutboxLogs();

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

builder.Services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.RabbitMq)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddMassTransit(o =>
{
    o.SetKebabCaseEndpointNameFormatter();
    o.AddConfigureEndpointsCallback((name, cfg) =>
    {
        if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
            rmq.SetQuorumQueue(3);
    });
    o.UsingRabbitMq((context, cfg) =>
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
    o.AddEntityFrameworkOutbox<ApplicationDbContext>(outboxConfigurator =>
    {
        outboxConfigurator.UsePostgres();
        outboxConfigurator.UseBusOutbox();
    });
});

builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;

builder.Services.AddB2C(b2COptions);
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

// Register specific repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IConsentRepository, ConsentRepository>();
builder.Services.AddScoped<ITermsRepository, TermsRepository>();

builder.Services.AddAuthorizationApi();
builder.Services.AddOptions<MitIDOptions>().BindConfiguration(MitIDOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IMitIDService, MitIDService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MitIDOptions>>().Value;
    client.BaseAddress = options.URI;
});

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
