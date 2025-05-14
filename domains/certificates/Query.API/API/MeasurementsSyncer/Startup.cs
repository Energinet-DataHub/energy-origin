using System;
using API.Configurations;
using API.MeasurementsSyncer.Clients.DataHub3;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using EnergyOrigin.DatahubFacade;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public static class Startup
{
    public static void AddMeasurementsSyncer(this IServiceCollection services)
    {
        services.MeasurementsSyncOptions();
        services.AddDataHub3Options();
        services.AddOptions<DataHubFacadeOptions>()
            .BindConfiguration(DataHubFacadeOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

    services.AddTransient<ITokenService, TokenService>();
        services.AddTransient<AuthHeaderHandler, AuthHeaderHandler>();

        services.AddScoped<MeasurementsSyncService>();
        services.AddScoped<SlidingWindowService>();
        services.AddScoped<ISlidingWindowState, SlidingWindowState>();
        services.AddScoped<IMeasurementSyncPublisher, MeasurementSyncPublisher>();
        services.AddScoped<EnergyMeasuredIntegrationEventMapper>();
        services.AddSingleton<IContractState, ContractState>();

        services.AddSingleton<IMeasurementSyncMetrics, MeasurementSyncMetrics>();
        services.AddHostedService<MeasurementsSyncerWorker>();

        services.AddGrpcClient<Meteringpoint.V1.Meteringpoint.MeteringpointClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
            o.Address = new Uri(options.GrpcUrl);
        });

        services.AddHttpClient<IDataHubFacadeClient, DataHubFacadeClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });

        services.AddHttpClient<IDataHub3Client, DataHub3Client>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHub3Options>>().Value;
            client.BaseAddress = new Uri(options.Url);
            client.Timeout = TimeSpan.FromSeconds(300); // Databricks can autoscale under high load, which can take a long time. So this is so we don't lose the call if that happens.
        }).AddHttpMessageHandler<AuthHeaderHandler>();
    }
}
