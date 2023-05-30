using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
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

    public async Task<List<DataSyncDto>> FetchMeasurements(CertificateIssuingContract contract,
        CancellationToken cancellationToken)
    {
        var dateFrom = await syncState.GetPeriodStartTime(contract);

        if (dateFrom == null)
        {
            logger.LogInformation("Not possible to get start date from sync state for {@contract}", contract);
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
                    contract.GSRN,
                    new Period(dateFrom.Value, nearestHour),
                    contract.MeteringPointOwner,
                    cancellationToken
                );

                logger.LogInformation(
                    "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                    result.Count,
                    contract.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(dateFrom.Value).ToString("o"),
                    DateTimeOffset.FromUnixTimeSeconds(nearestHour).ToString("o"));

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
