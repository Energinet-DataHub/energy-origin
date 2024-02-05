using System;
using API.Configurations;
using API.MeasurementsSyncer.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services)
    {
        services.MeasurementsSyncOptions();
        services.AddMeasurementsOptions();

        services.AddSingleton<MeasurementsSyncService>();
        services.AddSingleton<ISyncState, SyncState>();

        services.AddHostedService<MeasurementsSyncerWorker>();

        services.AddGrpcClient<Measurements.V1.Measurements.MeasurementsClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<MeasurementsOptions>>().Value;
            o.Address = new Uri(options.Url);
        });
    }
}
