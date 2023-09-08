using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using Marten;
using MassTransit;
using MeasurementEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly IDocumentStore documentStore;
    private readonly DataSyncService dataSyncService;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        IDocumentStore documentStore,
        IBus bus,
        DataSyncService dataSyncService)
    {
        this.bus = bus;
        this.logger = logger;
        this.documentStore = documentStore;
        this.dataSyncService = dataSyncService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanupResult = await documentStore.CleanupContracts(stoppingToken);
        logger.LogInformation("Deleted {deletionCount} contracts for GSRN {gsrn}", cleanupResult.deletionCount, cleanupResult.gsrn);
        var positionDeletionCount = await documentStore.MigrateSynchronizationPosition(stoppingToken);
        logger.LogInformation("Deleted ({deletionCount}) SyncPositions", positionDeletionCount);

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
            await using var querySession = documentStore.QuerySession();

            var allContracts = await querySession
                .Query<CertificateIssuingContract>()
                .ToListAsync(cancellationToken);

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

<<<<<<< HEAD
public static class ContractCleanup
{
    private const string BadMeteringPointInDemoEnvironment = "571313000000000200";

    public static async Task<(string gsrn, int deletionCount)> CleanupContracts(this IDocumentStore store, CancellationToken cancellationToken)
    {
        await using var session = store.OpenSession();

        var contractsForBadMeteringPoint = await session.Query<CertificateIssuingContract>()
            .Where(c => c.GSRN == BadMeteringPointInDemoEnvironment)
            .ToListAsync(cancellationToken);

        var owners = contractsForBadMeteringPoint.Select(c => c.MeteringPointOwner).Distinct();

        if (owners.Count() == 1)
            return (BadMeteringPointInDemoEnvironment, 0);

        var deletionCount = contractsForBadMeteringPoint.Count;

        foreach (var certificateIssuingContract in contractsForBadMeteringPoint)
        {
            session.Delete(certificateIssuingContract);
=======
public static class SynchronizationMigration
{
    public static async Task<int> MigrateSynchronizationPosition(this IDocumentStore store, CancellationToken cancellationToken)
    {
        await using var session = store.OpenSession();

        var allPositions = await session.Query<SyncPosition>().ToListAsync(cancellationToken);
        int deletedPositions = allPositions.Count;

        var synchronizationPositions = allPositions
            .GroupBy(p => p.GSRN)
            .Select(g => new SynchronizationPosition { GSRN = g.Key, SyncedTo = g.Max(p => p.SyncedTo) });

        session.Store(synchronizationPositions);

        foreach (var syncPosition in allPositions)
        {
            session.Delete(syncPosition);
>>>>>>> main
        }

        await session.SaveChangesAsync(cancellationToken);

<<<<<<< HEAD
        return (BadMeteringPointInDemoEnvironment, deletionCount);
=======
        return deletedPositions;
>>>>>>> main
    }
}
