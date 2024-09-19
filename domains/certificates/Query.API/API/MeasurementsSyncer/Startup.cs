using System;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public static class Startup
{
    public static void AddMeasurementsSyncer(this IServiceCollection services)
    {
        services.MeasurementsSyncOptions();
        services.AddMeasurementsOptions();
        services.AddMeteringPointsOptions();

        services.AddScoped<MeasurementsSyncService>();
        services.AddScoped<SlidingWindowService>();
        services.AddScoped<ISlidingWindowState, SlidingWindowState>();
        services.AddScoped<IMeasurementSyncPublisher, MeasurementSyncPublisher>();
        services.AddScoped<EnergyMeasuredIntegrationEventMapper>();
        services.AddSingleton<IContractState, ContractState>();

        services.AddSingleton<IMeasurementSyncMetrics, MeasurementSyncMetrics>();
        services.AddHostedService<MeasurementsSyncerWorker>();

        services.AddGrpcClient<Measurements.V1.Measurements.MeasurementsClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<MeasurementsOptions>>().Value;
            o.Address = new Uri(options.GrpcUrl);
        });

        services.AddGrpcClient<Meteringpoint.V1.Meteringpoint.MeteringpointClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<MeteringPointOptions>>().Value;
            o.Address = new Uri(options.GrpcUrl);
        });
    }
}
