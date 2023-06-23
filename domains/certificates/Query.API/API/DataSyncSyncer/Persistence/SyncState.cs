using System;
using System.Linq;
using System.Threading.Tasks;
using API.ContractService;
using Marten;
using Microsoft.Extensions.Logging;

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

    public async Task<long?> GetPeriodStartTime(CertificateIssuingContract contract)
    {
        try
        {
            await using var querySession = documentStore.QuerySession();

            var queryRes = querySession
                .Query<SyncPosition>()
                .Where(x => x.Gsrn == contract.GSRN)
                .ToList();

            long? newestDateTo = null;
            if (queryRes.Any())
            {
                newestDateTo = queryRes.Max(x => x.SyncedTo);
            }

            return newestDateTo == null
                ? contract.StartDate.ToUnixTimeSeconds()
                : Math.Max(newestDateTo.Value, contract.StartDate.ToUnixTimeSeconds());
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed reading from database. Exception: {exception}", e);
            return null;
        }
    }

    public async void SetSyncPosition(SyncPosition syncPosition)
    {
        await using var session = documentStore.LightweightSession();
        session.Store(syncPosition);
        await session.SaveChangesAsync();
    }
}
