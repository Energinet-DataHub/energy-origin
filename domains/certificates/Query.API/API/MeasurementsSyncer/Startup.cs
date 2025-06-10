using System;
using System.Net.Http;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using Energinet.DataHub.Measurements.Client;
using Energinet.DataHub.Measurements.Client.Extensions.DependencyInjection;
using Energinet.DataHub.Measurements.Client.Extensions.Options;
using Energinet.DataHub.Measurements.Client.ResponseParsers;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public static class Startup
{
    public static void AddMeasurementsSyncer(this IServiceCollection services)
    {
        services.MeasurementsSyncOptions();
        services.AddOptions<DataHub3Options>()
            .BindConfiguration(DataHub3Options.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
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

        services.AddHttpClient("Measurements", delegate (IServiceProvider serviceProvider, HttpClient httpClient)
        {
            var value = serviceProvider.GetRequiredService<IOptions<DataHub3Options>>().Value;
            httpClient.BaseAddress = new Uri(value.Url);
        }).AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddScoped<IMeasurementsForDateResponseParser, MeasurementsForDateResponseParser>();
        services.AddScoped<IMeasurementsClient, MeasurementsClient>();
        services.AddScoped<IMeasurementClient, MeasurementClient>();
    }
}
