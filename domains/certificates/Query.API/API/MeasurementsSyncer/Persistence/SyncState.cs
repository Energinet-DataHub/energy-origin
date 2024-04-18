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
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<SyncState> logger;

    public SyncState(ApplicationDbContext dbContext, ILogger<SyncState> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<long?> GetPeriodStartTime(MeteringPointSyncInfo syncInfo, CancellationToken cancellationToken)
    {
        try
        {
            var synchronizationPosition = await dbContext.SynchronizationPositions.FindAsync(syncInfo.GSRN, cancellationToken);

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

    public async Task SetSyncPosition(string gsrn, long syncedTo, CancellationToken cancellationToken)
    {
        var synchronizationPosition = await dbContext.SynchronizationPositions.FindAsync(gsrn, cancellationToken);
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

    public async Task<MeteringPointTimeSeriesSlidingWindow?> GetMeteringPointSlidingWindow(string gsrn, CancellationToken cancellationToken)
    {
        var slidingWindow = await dbContext.MeteringPointTimeSeriesSlidingWindows.FindAsync(gsrn);
        return slidingWindow;
    }

    public async Task UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow slidingWindow, CancellationToken cancellationToken)
    {
        var existingWindow = await GetMeteringPointSlidingWindow(slidingWindow.GSRN, cancellationToken);
        if (existingWindow is null)
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
        }
        else
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Update(slidingWindow);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken)
    {
        try
        {
            var allContracts = await dbContext.Contracts.AsNoTracking().ToListAsync(cancellationToken);

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
