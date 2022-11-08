using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Service;
using API.DataSyncSyncer.Service.Datasync;
using API.DataSyncSyncer.Service.Integration;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly IIntegrationEventBus integrationEventBus;
    private readonly IDataSync dataSync;
    private readonly List<MasterData> masterData;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        MockMasterDataCollection collection,
        IIntegrationEventBus queue,
        IDataSync dataSync
    )
    {
        this.logger = logger;
        masterData = collection.Data.ToList();
        integrationEventBus = queue;
        this.dataSync = dataSync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            var period = GetPeriod();

            foreach (var data in masterData.Where(it => !string.IsNullOrWhiteSpace(it.GSRN)))
            {
                var measurement = await dataSync.GetMeasurement(data.GSRN, period);
                await integrationEventBus.Produce(stoppingToken, measurement);
                logger.LogInformation("Produce energy measured event");
            }

            var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
            logger.LogInformation("Minutes to next hour {minutesToNextHour}", minutesToNextHour);
            await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), stoppingToken);
        }
    }

    private static Period GetPeriod()
    {
        var now = DateTimeOffset.UtcNow;
        var date = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            0,
            0,
            TimeSpan.Zero);

        return new Period(
            DateFrom: date.AddHours(-1).ToUnixTimeSeconds(),
            DateTo: date.ToUnixTimeSeconds()
        );
    }
}
