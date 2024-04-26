using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncerWorker : BackgroundService
{
    private readonly IContractState syncState;
    private readonly ILogger<MeasurementsSyncerWorker> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly MeasurementsSyncOptions options;

    public MeasurementsSyncerWorker(
        ILogger<MeasurementsSyncerWorker> logger,
        IContractState syncState,
        IOptions<MeasurementsSyncOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        this.syncState = syncState;
        this.logger = logger;
        this.scopeFactory = scopeFactory;
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
        try
        {
            var syncInfos = await syncState.GetSyncInfos(stoppingToken);

            using var outerScope = scopeFactory.CreateScope();
            var measurementSyncMetrics = outerScope.ServiceProvider.GetRequiredService<IMeasurementSyncMetrics>();
            measurementSyncMetrics.TimeSinceLastMeasurementSyncerRunDone(UnixTimestamp.Now().Seconds);

            var oldestSyncDate = syncInfos.Min(x => x.StartSyncDate);

            measurementSyncMetrics.TimePeriodForSearchingForAGSRNAssign(UnixTimestamp.Create(oldestSyncDate).Seconds);

            foreach (var syncInfo in syncInfos)
            {

                using var scope = scopeFactory.CreateScope();
                var scopedSyncService = scope.ServiceProvider.GetService<MeasurementsSyncService>()!;
                await scopedSyncService.HandleSingleSyncInfo(syncInfo, stoppingToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in MeasurementSyncer periodic task");
        }
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        try
        {
            var timeUntilNextHour = UnixTimestamp.Now().TimeUntilNextHour();
            logger.LogInformation("Sleeping until next full hour {TimeToNextHour}", timeUntilNextHour);
            await Task.Delay(timeUntilNextHour, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Sleep interrupted
        }
    }
}
