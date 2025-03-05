using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncerWorker : BackgroundService
{
    private readonly IContractState _contractState;
    private readonly ILogger<MeasurementsSyncerWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MeasurementsSyncOptions _options;

    public MeasurementsSyncerWorker(
        ILogger<MeasurementsSyncerWorker> logger,
        IContractState contractState,
        IOptions<MeasurementsSyncOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _contractState = contractState;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Disabled)
        {
            _logger.LogInformation("MeasurementSyncer is disabled!");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MeasurementSyncer running job");
            await PerformPeriodicTask(stoppingToken);
            await Sleep(stoppingToken);
        }

        _logger.LogInformation("MeasurementSyncer stopped");
    }

    private async Task PerformPeriodicTask(CancellationToken stoppingToken)
    {
        try
        {
            var syncInfos = await _contractState.GetSyncInfos(stoppingToken);

            if (!syncInfos.Any())
            {
                _logger.LogInformation("No sync infos found. Skipping sync");
                return;
            }

            try
            {
                foreach (var syncInfo in syncInfos)
                {
                    await HandleMeteringPoint(stoppingToken, syncInfo);
                }
            }
            finally
            {
                UpdateGauges();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in MeasurementSyncer periodic task");
        }
    }

    private async Task HandleMeteringPoint(CancellationToken stoppingToken, MeteringPointSyncInfo syncInfo)
    {
        using var scope = _scopeFactory.CreateScope();
        var measurementSyncMetrics = scope.ServiceProvider.GetRequiredService<IMeasurementSyncMetrics>();
        measurementSyncMetrics.AddNumberOfContractsBeingSynced(1);
        var scopedSyncService = scope.ServiceProvider.GetService<MeasurementsSyncService>()!;
        await scopedSyncService.HandleMeteringPoint(syncInfo, stoppingToken);
    }

    private void UpdateGauges()
    {
        using var outerScope = _scopeFactory.CreateScope();
        var measurementSyncMetrics = outerScope.ServiceProvider.GetRequiredService<IMeasurementSyncMetrics>();
        measurementSyncMetrics.UpdateGauges();
    }

    private async Task Sleep(CancellationToken cancellationToken)
    {
        if (_options.SleepType == MeasurementsSyncerSleepType.Hourly)
        {
            await SleepToNearestHour(cancellationToken);
        }
        else if (_options.SleepType == MeasurementsSyncerSleepType.EveryThirdSecond)
        {
            await Task.Delay(3000, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Sleep option {nameof(_options.SleepType)} has invalid value {_options.SleepType}");
        }
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        try
        {
            var timeUntilNextHour = UnixTimestamp.Now().TimeUntilNextHour();
            _logger.LogInformation("Sleeping until next full hour {TimeToNextHour}", timeUntilNextHour);
            await Task.Delay(timeUntilNextHour, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }
    }
}
