using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;

namespace API.DataSyncSyncer.Persistence;

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
}
