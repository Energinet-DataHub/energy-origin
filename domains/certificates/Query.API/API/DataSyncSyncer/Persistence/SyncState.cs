using Marten;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.DataSyncSyncer.Persistence;

public class SyncState : ISyncState
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<SyncState> logger;

    public SyncState(IDocumentStore documentStore, ILogger<SyncState> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task<long?> GetPeriodStartTime(MeteringPointSyncInfo syncInfo)
    {
        try
        {
            await using var querySession = documentStore.QuerySession();

            var synchronizationPosition = querySession.Load<SynchronizationPosition>(syncInfo.GSRN);
            
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
        await using var session = documentStore.LightweightSession();
        var synchronizationPosition = session.Load<SynchronizationPosition>(gsrn) ?? new SynchronizationPosition { GSRN = gsrn };

        synchronizationPosition.SyncedTo = syncedTo;

        session.Store(synchronizationPosition);
        await session.SaveChangesAsync();
    }
}
