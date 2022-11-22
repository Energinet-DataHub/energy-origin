using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using CertificateEvents.Primitives;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

public class DataSyncService
{
    private readonly IDataSyncClient client;
    private ISyncState? syncState;

    private readonly ILogger<DataSyncService> logger;

    public DataSyncService(IDataSyncClient client, ILogger<DataSyncService> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public void SetState(Dictionary<string, DateTimeOffset> state)
    {
        syncState = SyncStateFactory.CreateSyncState(state);
    }

    public async Task<List<DataSyncDto>> FetchMeasurements(string GSRN, string meteringPointOwner,
        CancellationToken cancellationToken)
    {
        var dateFrom = syncState!.GetPeriodStartTime(GSRN);

        var now = DateTimeOffset.UtcNow;
        var midnight = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var result = new List<DataSyncDto>();

        if (dateFrom < midnight)
        {
            try
            {
                result = await client.RequestAsync(
                    GSRN,
                    new Period(
                        DateFrom: dateFrom,
                        DateTo: midnight
                    ),
                    meteringPointOwner,
                    cancellationToken
                );
            }
            catch (Exception e)
            {
                logger.LogInformation("An error occured: {error}, no measurements were fetched", e.Message);
            }
        }

        syncState.SetNextPeriodStartTime(result, GSRN);
        return result;
    }
}
