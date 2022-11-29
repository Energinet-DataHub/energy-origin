using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using API.MasterDataService;
using API.MasterDataService.MockInput;
using IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly MasterDataMockInputCollection collection;
    private readonly DataSyncService dataSyncService;
    private readonly IMasterDataService masterDataService;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        MasterDataMockInputCollection collection,
        IMasterDataService masterDataService,
        IBus bus,
        DataSyncService dataSyncService)
    {
        this.bus = bus;
        this.logger = logger;
        this.collection = collection;
        this.dataSyncService = dataSyncService;
        this.masterDataService = masterDataService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var gsrn in collection.GetAllGsrns())
            {
                var masterData = await masterDataService.GetMasterData(gsrn);

                if (masterData == null)
                {
                    logger.LogInformation("No master data for {gsrn}", gsrn);
                    continue;
                }

                var measurements = await dataSyncService.FetchMeasurements(masterData,
                    cancellationToken);

                if (measurements.Any())
                {
                    await PublishIntegrationEvents(measurements, cancellationToken);
                }
            }

            await SleepToNearestHour(cancellationToken);
        }
    }

    private async Task PublishIntegrationEvents(List<DataSyncDto> measurements, CancellationToken cancellationToken)
    {
        var integrationsEvents = MapToIntegrationEvents(measurements);
        logger.LogInformation(
            "Publishing {numberOfEnergyMeasuredIntegrationEvents} energyMeasuredIntegrationEvents to the Integration Bus",
            integrationsEvents.Count
        );
        foreach (var @event in integrationsEvents)
        {
            await bus.Publish(@event, cancellationToken);
        }
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }

    private static List<EnergyMeasuredIntegrationEvent> MapToIntegrationEvents(List<DataSyncDto> measurements)
    {
        return measurements
            .Select(it => new EnergyMeasuredIntegrationEvent(
                    GSRN: it.GSRN,
                    DateFrom: it.DateFrom,
                    DateTo: it.DateTo,
                    Quantity: it.Quantity,
                    Quality: it.Quality
                )
            )
            .ToList();
    }
}
