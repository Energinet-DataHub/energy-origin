using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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

            //await using var querySession = documentStore.QuerySession();

            //var synchronizationPosition = querySession.Load<SynchronizationPosition>(syncInfo.GSRN);

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

    public async void SetSyncPosition(string gsrn, long syncedTo)
    {
        var dbContext = await factory.CreateDbContextAsync();

        var synchronizationPosition = await dbContext.SynchronizationPositions.FindAsync(gsrn) ?? new SynchronizationPosition { GSRN = gsrn };

        //await using var session = documentStore.LightweightSession();
        //var synchronizationPosition = session.Load<SynchronizationPosition>(gsrn) ?? new SynchronizationPosition { GSRN = gsrn };
        synchronizationPosition.SyncedTo = syncedTo;

        dbContext.Update(synchronizationPosition);
        await dbContext.SaveChangesAsync();
        //session.Store(synchronizationPosition);
        //await session.SaveChangesAsync();
    }
}
