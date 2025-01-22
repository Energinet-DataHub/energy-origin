using System;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using EnergyOrigin.Setup.Swagger;
using MassTransit.Logging;
using MassTransit.Monitoring;
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

public static class ServiceCollectionExtensions
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
                options.ApiVersionReader = new HeaderApiVersionReader("X-API-Version");

                options.Policies.Sunset(int.Parse(ApiVersions.Version20230101))
                    .Effective(new DateTimeOffset(2025, 03, 23, 0, 0, 0, TimeSpan.Zero));

                options.Policies.Sunset(int.Parse(ApiVersions.Version20240515))
                    .Effective(new DateTimeOffset(2025, 03, 23, 0, 0, 0, TimeSpan.Zero));
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

    public static IOpenTelemetryBuilder AddOpenTelemetryMetricsAndTracing(this IServiceCollection services, string serviceName,
        Uri oltpReceiverEndpoint)
    {
        var openTelemetryBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceInstanceId: Environment.MachineName));

        return openTelemetryBuilder
            .WithMetrics(meterProviderBuilder =>
                meterProviderBuilder
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint))
            .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddConsoleExporter()
                    .AddOtlpExporter(o => o.Endpoint = oltpReceiverEndpoint));
    }

    public static IOpenTelemetryBuilder AddOpenTelemetryMetricsAndTracingWithGrpc(this IServiceCollection services, string serviceName,
        Uri oltpReceiverEndpoint)
    {
        return services.AddOpenTelemetryMetricsAndTracing(serviceName, oltpReceiverEndpoint)
            .WithTracing(traceBuilder =>
            {
                traceBuilder.AddGrpcClientInstrumentation(grpcOptions =>
                {
                    grpcOptions.SuppressDownstreamInstrumentation = true;
                    grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        activity.SetTag("requestVersion", httpRequestMessage.Version);
                    grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                        activity.SetTag("responseVersion", httpResponseMessage.Version);
                });
            });
    }

    public static IOpenTelemetryBuilder AddOpenTelemetryMetricsAndTracingWithGrpcAndMassTransit(this IServiceCollection services,
        string serviceName, Uri oltpReceiverEndpoint)
    {
        return services.AddOpenTelemetryMetricsAndTracing(serviceName, oltpReceiverEndpoint)
            .WithMetrics(builder => builder.AddMeter(InstrumentationOptions.MeterName))
            .WithTracing(builder => builder.AddSource(DiagnosticHeaders.DefaultListenerName));
    }
}
