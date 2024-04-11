using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using DataContext;
using DataContext.Models;
using MassTransit;
using MeasurementEvents;
using Measurements.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

internal class MeasurementsSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<MeasurementsSyncerWorker> logger;
    private readonly IDbContextFactory<TransferDbContext> contextFactory;
    private readonly MeasurementsSyncService measurementsSyncService;
    private readonly MeasurementsSyncOptions options;

    public MeasurementsSyncerWorker(
        ILogger<MeasurementsSyncerWorker> logger,
        IDbContextFactory<TransferDbContext> contextFactory,
        IBus bus,
        MeasurementsSyncService measurementsSyncService,
        IOptions<MeasurementsSyncOptions> options)
    {
        this.bus = bus;
        this.logger = logger;
        this.contextFactory = contextFactory;
        this.measurementsSyncService = measurementsSyncService;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Disabled)
        {
            logger.LogInformation("DataSyncSyncer is disabled!");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var syncInfos = await GetSyncInfos(stoppingToken);
            foreach (var syncInfo in syncInfos)
            {
                var measurements = await measurementsSyncService.FetchMeasurements(syncInfo,
                    stoppingToken);

                if (measurements.Any())
                {
                    await PublishIntegrationEvents(measurements, stoppingToken);
                }
            }

            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var allContracts = await context.Contracts.AsNoTracking().ToListAsync(cancellationToken);

            //TODO: Currently the sync is only per GSRN/metering point, but should be changed to a combination of (GSRN, metering point owner). See https://github.com/Energinet-DataHub/energy-origin-issues/issues/1659 for more details
            var syncInfos = allContracts.GroupBy(c => c.GSRN)
                .Where(g => GetNumberOfOwners(g) == 1)
                .Select(g =>
                {
                    var oldestContract = g.OrderBy(c => c.StartDate).First();
                    var gsrn = g.Key;
                    return new MeteringPointSyncInfo(gsrn, oldestContract.StartDate, oldestContract.MeteringPointOwner);
                })
                .ToList();

            var contractsWithChangingOwnerForSameMeteringPoint = allContracts.GroupBy(c => c.GSRN)
                .Where(g => GetNumberOfOwners(g) > 1);

            if (contractsWithChangingOwnerForSameMeteringPoint.Any())
            {
                logger.LogWarning("Skipping sync of GSRN with multiple owners: {contractsWithChangingOwnerForSameMeteringPoint}", contractsWithChangingOwnerForSameMeteringPoint);
            }

            return syncInfos;
        }
        catch (Exception e)
        {
            logger.LogError("Failed fetching contracts. Exception: {e}", e);
            return new List<MeteringPointSyncInfo>();
        }
    }

    private async Task PublishIntegrationEvents(List<Measurement> measurements, CancellationToken cancellationToken)
    {
        var integrationsEvents = MapToIntegrationEvents(measurements);
        logger.LogInformation(
            "Publishing {numberOfEnergyMeasuredIntegrationEvents} energyMeasuredIntegrationEvents to the Integration Bus",
            integrationsEvents.Count);

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

    private static int GetNumberOfOwners(IGrouping<string, CertificateIssuingContract> g) =>
        g.Select(c => c.MeteringPointOwner).Distinct().Count();

    private static List<EnergyMeasuredIntegrationEvent> MapToIntegrationEvents(List<Measurement> measurements) =>
        measurements
            .Select(it => new EnergyMeasuredIntegrationEvent(
                    GSRN: it.Gsrn,
                    DateFrom: it.DateFrom,
                    DateTo: it.DateTo,
                    Quantity: it.Quantity,
                    Quality: MapQuality(it.Quality)
                )
            )
            .ToList();

    private static MeasurementQuality MapQuality(EnergyQuantityValueQuality q) =>
        q switch
        {
            EnergyQuantityValueQuality.Measured => MeasurementQuality.Measured,
            EnergyQuantityValueQuality.Estimated => MeasurementQuality.Estimated,
            EnergyQuantityValueQuality.Calculated => MeasurementQuality.Calculated,
            EnergyQuantityValueQuality.Revised => MeasurementQuality.Revised,
            _ => throw new ArgumentOutOfRangeException(nameof(q), q, null)
        };
}
