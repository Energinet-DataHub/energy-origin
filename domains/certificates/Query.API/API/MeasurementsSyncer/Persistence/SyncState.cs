using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer.Persistence;

public class SyncState : ISyncState
{
    private readonly IDbContextFactory<ApplicationDbContext> factory;
    private readonly ILogger<SyncState> logger;

    public SyncState(IDbContextFactory<ApplicationDbContext> factory, ILogger<SyncState> logger)
    {
        this.factory = factory;
        this.logger = logger;
    }

    public async Task<long?> GetPeriodStartTime(MeteringPointSyncInfo syncInfo)
    {
        try
        {
            var dbContext = await factory.CreateDbContextAsync();

            var synchronizationPosition = await dbContext.SynchronizationPositions.FindAsync(syncInfo.GSRN);

            return synchronizationPosition != null
                ? Math.Max(synchronizationPosition.SyncedTo, syncInfo.StartSyncDate.ToUnixTimeSeconds())
                : syncInfo.StartSyncDate.ToUnixTimeSeconds();
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed reading from database. Exception: {exception}", e);
            return null;
        }
    }

    public async Task SetSyncPosition(string gsrn, long syncedTo)
    {
        var dbContext = await factory.CreateDbContextAsync();

        var synchronizationPosition = await dbContext.SynchronizationPositions.FindAsync(gsrn);
        if (synchronizationPosition != null)
        {
            synchronizationPosition.SyncedTo = syncedTo;
            dbContext.Update(synchronizationPosition);
        }
        else
        {
            synchronizationPosition = new SynchronizationPosition { GSRN = gsrn, SyncedTo = syncedTo };
            await dbContext.AddAsync(synchronizationPosition);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<MeteringPointTimeSeriesSlidingWindow?> GetMeteringPointSlidingWindow(string gsrn)
    {
        var dbContext = await factory.CreateDbContextAsync();

        var slidingWindow = await dbContext.MeteringPointTimeSeriesSlidingWindows.FindAsync(gsrn);

        return slidingWindow;
    }

    public async Task UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow slidingWindow)
    {
        var dbContext = await factory.CreateDbContextAsync();

        dbContext.MeteringPointTimeSeriesSlidingWindows.Update(slidingWindow);

        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await factory.CreateDbContextAsync(cancellationToken);

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
                logger.LogWarning("Skipping sync of GSRN with multiple owners: {contractsWithChangingOwnerForSameMeteringPoint}",
                    contractsWithChangingOwnerForSameMeteringPoint);
            }

            return syncInfos;
        }
        catch (Exception e)
        {
            logger.LogError("Failed fetching contracts. Exception: {e}", e);
            return new List<MeteringPointSyncInfo>();
        }
    }

    private static int GetNumberOfOwners(IGrouping<string, CertificateIssuingContract> g)
    {
        return g.Select(c => c.MeteringPointOwner).Distinct().Count();
    }
}
