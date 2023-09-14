using API.ContractService;
using API.Data;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Migration;
using MassTransit;
using MeasurementEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly IDbContextFactory<ApplicationDbContext> contextFactory;
    private readonly DataSyncService dataSyncService;
    private readonly MartenMigration martenMigration;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IBus bus,
        MartenMigration martenMigration,
        DataSyncService dataSyncService)
    {
        this.bus = bus;
        this.logger = logger;
        this.contextFactory = contextFactory;
        this.dataSyncService = dataSyncService;
        this.martenMigration = martenMigration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await martenMigration.Migrate();

        var cleanupResult = await contextFactory.CleanupContracts(stoppingToken);
        logger.LogInformation("Deleted {deletionCount} contracts for GSRN {gsrn}", cleanupResult.deletionCount, cleanupResult.gsrn);

        while (!stoppingToken.IsCancellationRequested)
        {
            var syncInfos = await GetSyncInfos(stoppingToken);
            foreach (var syncInfo in syncInfos)
            {
                var measurements = await dataSyncService.FetchMeasurements(syncInfo,
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

            var allContracts = await context.Contracts.ToListAsync(cancellationToken);

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
            logger.LogWarning("Failed fetching contracts. Exception: {e}", e);
            return new List<MeteringPointSyncInfo>();
        }
    }

    private async Task PublishIntegrationEvents(List<DataSyncDto> measurements, CancellationToken cancellationToken)
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

    private static List<EnergyMeasuredIntegrationEvent> MapToIntegrationEvents(List<DataSyncDto> measurements) =>
        measurements
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

public static class ContractCleanup
{
    private const string BadMeteringPointInDemoEnvironment = "571313000000000200";

    public static async Task<(string gsrn, int deletionCount)> CleanupContracts(this IDbContextFactory<ApplicationDbContext> contextFactory, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var contractsForBadMeteringPoint = await EntityFrameworkQueryableExtensions.ToListAsync(context.Contracts
                .Where(c => c.GSRN == BadMeteringPointInDemoEnvironment), cancellationToken);

        var owners = contractsForBadMeteringPoint.Select(c => c.MeteringPointOwner).Distinct();

        if (owners.Count() == 1)
            return (BadMeteringPointInDemoEnvironment, 0);

        var deletionCount = contractsForBadMeteringPoint.Count;

        foreach (var certificateIssuingContract in contractsForBadMeteringPoint)
        {
            context.Remove(certificateIssuingContract);
        }

        await context.SaveChangesAsync(cancellationToken);

        return (BadMeteringPointInDemoEnvironment, deletionCount);
    }
}
