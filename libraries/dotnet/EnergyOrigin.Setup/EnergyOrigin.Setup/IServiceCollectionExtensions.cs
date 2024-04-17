using System;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using EnergyOrigin.Setup.Swagger;
using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnergyOrigin.Setup;

public static class IServiceCollectionExtensions
{
    public static void AddSwagger(this IServiceCollection services, string subsystemName)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>>(s =>
            new ConfigureSwaggerOptions(s.GetRequiredService<IApiVersionDescriptionProvider>(), subsystemName));
    }

    public static void AddVersioningToApi(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader("EO_API_VERSION");
            })
            .AddMvc()
            .AddApiExplorer();
    }

    public static void AddControllersWithEnumsAsStrings(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddControllers()
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    }

    public static void AddOpenTelemetryMetricsAndTracing(this WebApplicationBuilder builder, string serviceName, Uri oltpReceiverEndpoint, Action<MeterProviderBuilder> meterProviderBuilderAction)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName))
            .WithMetrics(meterProviderBuilder =>
                meterProviderBuilder
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint))
            .WithMetrics(meterProviderBuilderAction)
            .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddSource(DiagnosticHeaders.DefaultListenerName)
                    .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint));
    }

    public static void AddOpenTelemetryMetricsAndTracingWithGrpc(this IServiceCollection services, string serviceName, Uri oltpReceiverEndpoint)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName))
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint))
            .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddGrpcClientInstrumentation(grpcOptions =>
                    {
                        grpcOptions.SuppressDownstreamInstrumentation = true;
                        grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                            activity.SetTag("requestVersion", httpRequestMessage.Version);
                        grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                            activity.SetTag("responseVersion", httpResponseMessage.Version);
                    })
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint));
    }
    public static void AddOpenTelemetryMetricsAndTracingWithGrpcAndMassTransit(this IServiceCollection services, Action<ResourceBuilder> configResource, Uri oltpReceiverEndpoint)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(configResource)
            .WithMetrics(meterProviderBuilder =>
                meterProviderBuilder
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter(InstrumentationOptions.MeterName)
                    .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint))
            .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddGrpcClientInstrumentation(grpcOptions =>
                    {
                        grpcOptions.SuppressDownstreamInstrumentation = true;
                        grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                            activity.SetTag("requestVersion", httpRequestMessage.Version);
                        grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                            activity.SetTag("responseVersion", httpResponseMessage.Version);
                    })
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddSource(DiagnosticHeaders.DefaultListenerName)
                    .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint));
    }
}
