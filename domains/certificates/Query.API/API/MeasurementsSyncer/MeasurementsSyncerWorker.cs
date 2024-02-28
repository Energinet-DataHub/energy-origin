using System;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncerWorker : BackgroundService
{
    private readonly ISyncState syncState;
    private readonly ILogger<MeasurementsSyncerWorker> logger;
    private readonly MeasurementsSyncService measurementsSyncService;
    private readonly MeasurementsSyncOptions options;

    public MeasurementsSyncerWorker(
        ILogger<MeasurementsSyncerWorker> logger,
        ISyncState syncState,
        MeasurementsSyncService measurementsSyncService,
        IOptions<MeasurementsSyncOptions> options)
    {
        this.syncState = syncState;
        this.logger = logger;
        this.measurementsSyncService = measurementsSyncService;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Disabled)
        {
            logger.LogInformation("MeasurementSyncer is disabled!");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("MeasurementSyncer running job");
            await PerformPeriodicTask(stoppingToken);
            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task PerformPeriodicTask(CancellationToken stoppingToken)
    {
        var syncInfos = await syncState.GetSyncInfos(stoppingToken);
        foreach (var syncInfo in syncInfos)
        {
            var slidingWindow = await GetSlidingWindow(syncInfo);

            if (slidingWindow is null)
            {
                logger.LogInformation("Not possible to get start date from sync state for {@syncInfo}", syncInfo);
                continue;
            }

            await measurementsSyncService.FetchAndPublishMeasurements(syncInfo.MeteringPointOwner, slidingWindow, stoppingToken);
        }
    }

    private async Task<MeteringPointTimeSeriesSlidingWindow?> GetSlidingWindow(MeteringPointSyncInfo syncInfo)
    {
        var existingSlidingWindow = await syncState.GetMeteringPointSlidingWindow(syncInfo.GSRN);
        if (existingSlidingWindow is not null)
        {
            return existingSlidingWindow;
        }

        var existingSynchronizationPoint = await syncState.GetPeriodStartTime(syncInfo);
        if (existingSynchronizationPoint is not null)
        {
            return MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.GSRN, UnixTimestamp.Create(existingSynchronizationPoint.Value));
        }

        return null;
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = UnixTimestamp.Now().MinutesUntilNextHour();
        logger.LogInformation("Sleeping until next full hour {MinutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }
}
