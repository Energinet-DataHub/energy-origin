using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncerWorker(
    ILogger<MeasurementsSyncerWorker> logger,
    IContractState contractState,
    IOptions<MeasurementsSyncOptions> options,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly MeasurementsSyncOptions _options = options.Value;
    private ITimer? _timer;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Disabled)
        {
            logger.LogInformation("MeasurementSyncer is disabled!");
            return Task.CompletedTask;
        }

        _timer = timeProvider.CreateTimer(
            callback: TimerCallback,
            state: this,
            dueTime: TimeSpan.Zero,
            period: GetTimerPeriod()
        );

        return Task.CompletedTask;
    }

    private TimeSpan GetTimerPeriod()
    {
        return _options.SleepType switch
        {
            MeasurementsSyncerSleepType.Hourly => TimeSpan.FromHours(1),
            MeasurementsSyncerSleepType.EveryThirdSecond => TimeSpan.FromSeconds(3),
            _ => throw new InvalidOperationException($"Sleep option {_options.SleepType} has invalid value")
        };
    }

    private async void TimerCallback(object? state)
    {
        var worker = (MeasurementsSyncerWorker)state!;

        try
        {
            await worker.PerformPeriodicTask(CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred in MeasurementSyncer task");
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

            foreach (var syncInfo in syncInfos)
            {
                await HandleMeteringPoint(stoppingToken, syncInfo);
            }

            UpdateGauges();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in MeasurementSyncer periodic task");
        }
    }

    private async Task HandleMeteringPoint(CancellationToken stoppingToken, MeteringPointSyncInfo syncInfo)
    {
        using var scope = scopeFactory.CreateScope();
        var measurementSyncMetrics = scope.ServiceProvider.GetRequiredService<IMeasurementSyncMetrics>();
        measurementSyncMetrics.AddNumberOfContractsBeingSynced(1);
        var scopedSyncService = scope.ServiceProvider.GetService<MeasurementsSyncService>()!;
        await scopedSyncService.HandleMeteringPoint(syncInfo, stoppingToken);
    }

    private void UpdateGauges()
    {
        using var scope = scopeFactory.CreateScope();
        var measurementSyncMetrics = scope.ServiceProvider.GetRequiredService<IMeasurementSyncMetrics>();
        measurementSyncMetrics.UpdateGauges();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_timer != null)
        {
            await _timer.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
