using System;
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
}
