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
using OpenTelemetry.Exporter;
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
                .AddService(serviceName: serviceName, serviceInstanceId: Environment.MachineName)
                .AddContainerDetector()
                .AddHostDetector());

        return openTelemetryBuilder
            .WithMetrics(meterProviderBuilder =>
                meterProviderBuilder
                    .SetExemplarFilter(ExemplarFilterType.TraceBased)
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = oltpReceiverEndpoint;
                        exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                    }))
            .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddSource(serviceName)
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = oltpReceiverEndpoint;
                        exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                    }));
    }

    public static IOpenTelemetryBuilder AddOpenTelemetryMetricsAndTracingWithGrpcAndMassTransit(this IServiceCollection services,
        string serviceName, Uri oltpReceiverEndpoint)
    {
        return services.AddOpenTelemetryMetricsAndTracing(serviceName, oltpReceiverEndpoint)
            .WithMetrics(builder => builder.AddMeter(InstrumentationOptions.MeterName))
            .WithTracing(builder => builder.AddSource(DiagnosticHeaders.DefaultListenerName));
    }
}
