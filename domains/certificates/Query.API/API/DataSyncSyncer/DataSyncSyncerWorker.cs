using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Service.Datasync;
using API.DataSyncSyncer.Service.Integration;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;
using IdentityServer4.Extensions;
using IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly IIntegrationEventBus integrationEventBus;
    private readonly IDataSync dataSync;
    private readonly List<MasterData> masterData;
    private readonly Dictionary<string, DateTimeOffset> periodStartTimeDictionary;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        MockMasterDataCollection collection,
        IBus bus,
        IDataSync dataSync
    )
    {
        this.bus = bus;
        this.logger = logger;
        masterData = collection.Data.ToList();
        this.dataSync = dataSync;
        periodStartTimeDictionary = masterData
            .Where(it => !string.IsNullOrWhiteSpace(it.GSRN))
            .ToDictionary(m => m.GSRN, m => m.MeteringPointOnboardedStartDate);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SleepToNearestHour(stoppingToken);

            foreach (var data in masterData)
            {
                var measurements = await FetchMeasurements(data.GSRN, data.MeteringPointOwner);
                SetNextPeriodStartTime(measurements, data.GSRN);

                var integrationsEvents = MapToIntegrationEvents(measurements);
                await integrationEventBus.Produce(stoppingToken, integrationsEvents);
                logger.LogInformation("Produce energy measured event");
            }
        }
    }

    private async Task SleepToNearestHour(CancellationToken stoppingToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Minutes to next hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), stoppingToken);
    }

    private async Task<List<DataSyncDto>> FetchMeasurements(string GSRN, string meteringPointOwner)
    {
        var dateFrom = periodStartTimeDictionary[GSRN].ToUnixTimeSeconds();

        var now = DateTimeOffset.UtcNow;
        var midnight = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        if (dateFrom < midnight)
        {
            return await dataSync.GetMeasurement(
                GSRN,
                new Period(
                    DateFrom: dateFrom,
                    DateTo: midnight
                ),
                meteringPointOwner
            );
        }

        return new List<DataSyncDto>();
    }

    private void SetNextPeriodStartTime(List<DataSyncDto> measurements, string GSRN)
    {
        if (measurements.IsNullOrEmpty())
        {
            return;
        }
        var newestMeasurement = measurements.Max(m => m.DateTo);
        periodStartTimeDictionary[GSRN] = DateTimeOffset.FromUnixTimeSeconds(newestMeasurement);
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
