using System;
using System.Linq;
using System.Threading.Tasks;
using API.MasterDataService;
using API.Query.API.Projections;
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

    public async Task<long?> GetPeriodStartTime(MasterData masterData)
    {
        try
        {
            await using var querySession = documentStore.QuerySession();

            var projection = await querySession.LoadAsync<CertificatesByOwnerView>(masterData.MeteringPointOwner);
            if (projection == null)
                return masterData.MeteringPointOnboardedStartDate.ToUnixTimeSeconds();

            var maxDateTo = projection.Certificates.Values
                .Where(c => masterData.GSRN.Equals(c.GSRN, StringComparison.InvariantCultureIgnoreCase))
                .Select(c => c.DateTo)
                .DefaultIfEmpty(0)
                .Max();

            return Math.Max(maxDateTo, masterData.MeteringPointOnboardedStartDate.ToUnixTimeSeconds());
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed reading from database. Exception: {exception}", e);
            return null;
        }
    }
}
