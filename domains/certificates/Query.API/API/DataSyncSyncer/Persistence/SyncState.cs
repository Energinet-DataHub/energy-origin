using System;
using System.Linq;
using System.Threading.Tasks;
using API.ContractService;
using CertificateEvents;
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

            var projection = await querySession.LoadAsync<SyncStateView>(contract.GSRN);

            return projection == null
                ? contract.StartDate.ToUnixTimeSeconds()
                : Math.Max(projection.SyncDateTo, contract.StartDate.ToUnixTimeSeconds());
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed reading from database. Exception: {exception}", e);
            return null;
        }
    }

    public async Task<long?> GetPeriodStartTime2(CertificateIssuingContract contract)
    {
        try
        {
            await using var querySession = documentStore.QuerySession();

            var queryRes = querySession.Query<ProductionCertificateCreated>().Where(x => x.ShieldedGSRN.Value == contract.GSRN);

            var newestDateTo = queryRes.Max(x => x.Period.DateTo);

            return newestDateTo == null
                ? contract.StartDate.ToUnixTimeSeconds()
                : Math.Max(newestDateTo, contract.StartDate.ToUnixTimeSeconds());
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed reading from database. Exception: {exception}", e);
            return null;
        }
    }
}
