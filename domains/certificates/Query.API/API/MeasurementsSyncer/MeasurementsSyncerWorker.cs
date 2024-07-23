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
    private readonly IContractState contractState;
    private readonly ILogger<MeasurementsSyncerWorker> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly MeasurementsSyncOptions options;

    public MeasurementsSyncerWorker(
        ILogger<MeasurementsSyncerWorker> logger,
        IContractState contractState,
        IOptions<MeasurementsSyncOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        this.contractState = contractState;
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
            await Sleep(stoppingToken);
        }
    }

    private async Task PerformPeriodicTask(CancellationToken stoppingToken)
    {
        try
        {
            var syncInfos = await contractState.GetSyncInfos(stoppingToken);

            if (!syncInfos.Any())
            {
                logger.LogInformation("No sync infos found. Skipping sync");
                return;
            }

            using var outerScope = scopeFactory.CreateScope();
            var measurementSyncMetrics = outerScope.ServiceProvider.GetRequiredService<IMeasurementSyncMetrics>();
            measurementSyncMetrics.UpdateTimeSinceLastMeasurementSyncerRun(UnixTimestamp.Now().Seconds);

            var oldestSyncDate = syncInfos.Min(x => x.StartSyncDate);

            measurementSyncMetrics.UpdateTimePeriodForSearchingForGSRN(UnixTimestamp.Create(oldestSyncDate).Seconds);
            measurementSyncMetrics.AddNumberOfRecordsBeingSynced(syncInfos.Count);

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

    private async Task Sleep(CancellationToken cancellationToken)
    {
        if (options.SleepType == MeasurementsSyncerSleepType.Hourly)
        {
            await SleepToNearestHour(cancellationToken);
        }
        else if (options.SleepType == MeasurementsSyncerSleepType.EveryThirdSecond)
        {
            await Task.Delay(3000, cancellationToken);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(options.SleepType), options.SleepType, "Unknown sleep type");
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
