using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using CertificateValueObjects;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

public class DataSyncService
{
    private readonly IDataSyncClientFactory factory;
    private readonly ISyncState syncState;
    private readonly ILogger<DataSyncService> logger;

    public DataSyncService(IDataSyncClientFactory factory, ILogger<DataSyncService> logger, ISyncState syncState)
    {
        this.factory = factory;
        this.logger = logger;
        this.syncState = syncState;
    }

    public async Task<List<DataSyncDto>> FetchMeasurements(MeteringPointSyncInfo syncInfo,
        CancellationToken cancellationToken)
    {
        var dateFrom = await syncState.GetPeriodStartTime(syncInfo);

        if (dateFrom == null)
        {
            logger.LogInformation("Not possible to get start date from sync state for {@syncInfo}", syncInfo);
            return new();
        }

        var now = DateTimeOffset.UtcNow;
        var nearestHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        if (dateFrom < nearestHour)
        {
            try
            {
                var client = factory.CreateClient();
                var result = await client.RequestAsync(
                    syncInfo.GSRN,
                    new Period(dateFrom.Value, nearestHour),
                    syncInfo.MeteringPointOwner,
                    cancellationToken
                );

                logger.LogInformation(
                    "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                    result.Count,
                    syncInfo.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(dateFrom.Value).ToString("o"),
                    DateTimeOffset.FromUnixTimeSeconds(nearestHour).ToString("o"));

                if (result.Any())
                {
                    var nextSyncPosition = result.Max(m => m.DateTo);
                    syncState.SetSyncPosition(new SyncPosition(Guid.NewGuid(), syncInfo.GSRN, nextSyncPosition));
                }

                return result;
            }
            catch (Exception e)
            {
                logger.LogInformation("An error occured: {error}, no measurements were fetched", e.Message);
            }
        }

        return new();
    }
}
