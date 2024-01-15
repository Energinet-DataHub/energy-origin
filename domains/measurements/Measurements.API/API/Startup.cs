using API.Options;
using API.Services;
using API.Shared.Swagger;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Linq;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Hosting;
using Asp.Versioning.ApiExplorer;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

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
        services.AddHealthChecks();

        services.AddOptions<DataSyncOptions>().BindConfiguration(DataSyncOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        services.AddTransient(x => x.GetRequiredService<IOptions<DataSyncOptions>>().Value);

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
        services.AddFluentValidationAutoValidation();

        services.AddEndpointsApiExplorer();

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();

        services.AddLogging();

        services.AddHttpClient<IDataSyncService, DataSyncService>(client => services.Configure<DataSyncOptions>(x => client.BaseAddress = x.Endpoint));
        services.AddScoped<IMeasurementsService, MeasurementsService>();
        services.AddScoped<IAggregator, MeasurementAggregation>();

        services.AddOptions<DataHubFacadeOptions>().BindConfiguration(DataHubFacadeOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
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
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = false;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new HeaderApiVersionReader("EO_API_VERSION");
        })
            .AddMvc()
            .AddApiExplorer();

        var tokenValidationOptions = _configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
        services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

        ValidationParameters validationParameters = new ValidationParameters(tokenValidationOptions.PublicKey);
        validationParameters.ValidIssuer = tokenValidationOptions.Issuer;
        validationParameters.ValidAudience = tokenValidationOptions.Audience;
        services.AddAuthentication().AddJwtBearer((Action<JwtBearerOptions>)(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = (TokenValidationParameters)validationParameters;
        }));
        services.AddAuthorization((Action<AuthorizationOptions>)(options => options.AddPolicy("requires-company", (Action<AuthorizationPolicyBuilder>)(policy => policy.RequireClaim("tin")))));
        services.AddGrpc();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwagger(o => o.RouteTemplate = "api-docs/measurements/{documentName}/swagger.json");
        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(
                options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(x => x.GroupName))
                    {
                        options.SwaggerEndpoint(
                            $"/api-docs/measurements/{description.GroupName}/swagger.json",
                            $"API v{description.GroupName}");
                    }
                });
        }

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
