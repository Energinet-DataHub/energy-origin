using System;
using API.Measurements.gRPC.V1.Services;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Consumer;
using API.Options;
using Contracts;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Metertimeseries.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API;

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
            options => options.UseNpgsql(
                _configuration.GetConnectionString("Postgres"),
                providerOptions => providerOptions.EnableRetryOnFailure()
            ),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<ApplicationDbContext>();

        services.AddSingleton<IConnection>(sp =>
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
        })
        .AddHealthChecks()
        .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!)
        .AddRabbitMQ();

        services.AddControllersWithEnumsAsStrings();

        services.AddHttpContextAccessor();

        services.AddOptions<OtlpOptions>()
            .BindConfiguration(OtlpOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
        services.AddFluentValidationAutoValidation();

        services.AddEndpointsApiExplorer();

        services.AddSwagger("measurements");
        services.AddSwaggerGen(c =>
        {
            c.DocumentFilter<AddMeasurementsTagDocumentFilter>();
        });

        services.AddLogging();

        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.RabbitMq)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<RetryOptions>().BindConfiguration(RetryOptions.Retry).ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumer<TermsConsumer, TermsConsumerErrorDefinition>();

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
        services.AddGrpcClient<MeterTimeSeries.MeterTimeSeriesClient>((sp, o) =>
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

        var b2COptions = _configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
        services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        services.AddB2C(b2COptions);

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
            endpoints.MapGrpcService<MeasurementsService>();
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });
    }
}

public class AddMeasurementsTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = "Measurements",
            Description = """
                          Measurements in Energy Track & Trace provides endpoint for getting measurements and status for syncronization status.
                          The status can either be:

                          Pending,
                          Created

                          Pending is the state when a company has accepted terms and relation between down stream system is being made.
                          Once the down stream system has accepted the relation the status will change to Created. First then we will start fetching data for an organization. This process is automated and should take no more than few minutes.
                          """
        });
    }
}
