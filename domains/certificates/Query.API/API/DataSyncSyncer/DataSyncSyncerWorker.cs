using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using API.MasterDataService;
using CertificateEvents.Primitives;
using IntegrationEvents;
using Marten;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly List<MasterData> masterData;
    private readonly DataSyncService dataSyncService;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        MockMasterDataCollection collection,
        IBus bus,
        DataSyncService dataSyncService
    )
    {
        this.bus = bus;
        this.logger = logger;
        this.dataSyncService = dataSyncService;

        masterData = collection.Data.ToList();
        var periodStartTimeDictionary = masterData
            .Where(it => !string.IsNullOrWhiteSpace(it.GSRN))
            .ToDictionary(m => m.GSRN, m => m.MeteringPointOnboardedStartDate);
        dataSyncService.SetState(periodStartTimeDictionary);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await SleepToNearestHour(cancellationToken);

            foreach (var data in masterData)
            {
                var measurements = await dataSyncService.FetchMeasurements(data.GSRN, data.MeteringPointOwner,
                    cancellationToken);

                if (measurements.Any())
                {
                    var integrationsEvents = MapToIntegrationEvents(measurements);
                    await bus.Publish(integrationsEvents, cancellationToken);
                }
            }
        }
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        //var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        var minutesToNextHour = 1;
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
