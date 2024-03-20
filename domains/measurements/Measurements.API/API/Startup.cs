using System;
using API.Options;
using EnergyOrigin.TokenValidation.Options;
using Microsoft.Extensions.Options;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Consumer;
using FluentValidation;
using FluentValidation.AspNetCore;
using EnergyOrigin.Setup;
using Contracts;
using EnergyOrigin.TokenValidation.Utilities;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace API;

//We startup this way so we can use the TestServerFixture class for integration testing gRPC services
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(
            options => options.UseNpgsql(_configuration.GetConnectionString("Postgres")),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<ApplicationDbContext>();
        services.AddHealthChecks()
            .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

        services.AddControllersWithEnumsAsStrings();

        services.AddOptions<OtlpOptions>()
            .BindConfiguration(OtlpOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
        services.AddFluentValidationAutoValidation();

        services.AddEndpointsApiExplorer();

        services.AddSwagger("measurements");
        services.AddSwaggerGen();

        services.AddLogging();

        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.RabbitMq)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumer<TermsConsumer, TermsConsumerErrorDefinition>();

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
        });

        var otlpConfiguration = _configuration.GetSection(OtlpOptions.Prefix);
        var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

        services.AddOpenTelemetryMetricsAndTracingWithGrpc("Measurements.API", otlpOptions.ReceiverEndpoint);

        services.AddOptions<DataHubFacadeOptions>()
            .BindConfiguration(DataHubFacadeOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddGrpcClient<Meteringpoint.V1.Meteringpoint.MeteringpointClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
            o.Address = new Uri(options.Url);
        });
        services.AddGrpcClient<Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
            o.Address = new Uri(options.Url);
        });

        services.AddGrpcClient<Relation.V1.Relation.RelationClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
            o.Address = new Uri(options.Url);
        });

        services.AddVersioningToApi();

        services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix)
            .ValidateDataAnnotations().ValidateOnStart();
        var tokenValidationOptions =
            _configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
        services.AddTokenValidation(tokenValidationOptions);

        services.AddGrpc();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.AddSwagger(env, "measurements");

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<API.Measurements.gRPC.V1.Services.MeasurementsService>();
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });
    }
}
