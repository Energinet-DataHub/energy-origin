using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using API.MasterDataService;
using CertificateEvents.Primitives;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

public class DataSyncService
{
    private readonly IDataSyncClient client;
    private readonly ISyncState syncState;

    private readonly ILogger<DataSyncService> logger;

    public DataSyncService(IDataSyncClient client, ILogger<DataSyncService> logger, ISyncState syncState)
    {
        this.client = client;
        this.logger = logger;
        this.syncState = syncState;
    }

    public async Task<List<DataSyncDto>> FetchMeasurements(MasterData masterData,
        CancellationToken cancellationToken)
    {
        var dateFrom = await syncState.GetPeriodStartTime(masterData);

        if (dateFrom == null)
        {
            logger.LogInformation("Not possible to get start date from sync state for {masterData}", masterData);
            return new();
        }

        var now = DateTimeOffset.UtcNow;
        var midnight = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        if (dateFrom < midnight)
        {
            try
            {
                return await client.RequestAsync(
                    masterData.GSRN,
                    new Period(
                        DateFrom: dateFrom.Value,
                        DateTo: midnight
                    ),
                    masterData.MeteringPointOwner,
                    cancellationToken
                );
            }
            catch (Exception e)
            {
                logger.LogInformation("An error occured: {error}, no measurements were fetched", e.Message);
            }
        }

        return new();
    }
}
