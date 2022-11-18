using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using CertificateEvents.Primitives;
using Marten;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

public class DataSyncService
{
    private readonly IDataSyncClient client;
    private readonly IState state;
    private readonly ILogger<DataSyncService> logger;
    private Dictionary<string, DateTimeOffset>? periodStartTimeDictionary;

    public DataSyncService(IDataSyncClient client, ILogger<DataSyncService> logger, IState state)
    {
        this.client = client;
        this.logger = logger;
        this.state = state;
    }

    public void SetState(Dictionary<string, DateTimeOffset> state)
    {
        this.state.SetState(state);
    }

    public async Task<List<DataSyncDto>> FetchMeasurements(string GSRN, string meteringPointOwner,
        CancellationToken cancellationToken)
    {
        var dateFrom = state.GetPeriodStartTime(GSRN);

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

        state.SetNextPeriodStartTime(result, GSRN);
        return result;
    }
}
