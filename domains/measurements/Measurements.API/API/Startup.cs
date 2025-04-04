using System;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Consumer;
using API.Options;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Health;
using EnergyOrigin.Setup.OpenTelemetry;
using EnergyOrigin.Setup.RabbitMq;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
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

        services.AddDefaultHealthChecks();

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
        services.AddSwaggerGen(c => { c.DocumentFilter<AddMeasurementsTagDocumentFilter>(); });

        services.AddLogging();

        services.AddMassTransitAndRabbitMq<ApplicationDbContext>(x =>
        {
            x.AddConsumer<TermsConsumer, TermsConsumerErrorDefinition>();
        });

        services.AddOptions<RetryOptions>().BindConfiguration(RetryOptions.Retry).ValidateDataAnnotations()
            .ValidateOnStart();

        var otlpConfiguration = _configuration.GetSection(OtlpOptions.Prefix);
        var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

        services.AddOpenTelemetryMetricsAndTracing("Measurements.API", otlpOptions.ReceiverEndpoint);

        services.AddOptions<DataHubFacadeOptions>()
            .BindConfiguration(DataHubFacadeOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddGrpcClient<Meteringpoint.V1.Meteringpoint.MeteringpointClient>((sp, o) =>
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
            endpoints.MapControllers();
            endpoints.MapDefaultHealthChecks();
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
