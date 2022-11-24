using System;
using System.Collections.Generic;
using System.Linq;
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
        var dateFrom = syncState.GetPeriodStartTime(masterData.GSRN, masterData.MeteringPointOnboardedStartDate);

        var now = DateTimeOffset.UtcNow;
        var midnight = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var result = new List<DataSyncDto>();

        if (dateFrom < midnight)
        {
            try
            {
                result = await client.RequestAsync(
                    masterData.GSRN,
                    new Period(
                        DateFrom: dateFrom,
                        DateTo: midnight
                    ),
                    masterData.MeteringPointOwner,
                    cancellationToken
                );
                logger.LogInformation(
                    "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                    result.Count,
                    masterData.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(dateFrom).ToString("o"),
                    DateTimeOffset.FromUnixTimeSeconds(midnight).ToString("o")
                );
            }
            catch (Exception e)
            {
                logger.LogInformation("An error occured: {error}, no measurements were fetched", e.Message);
            }
        }

        syncState.SetNextPeriodStartTime(result, masterData.GSRN);
        return result;
    }
}
