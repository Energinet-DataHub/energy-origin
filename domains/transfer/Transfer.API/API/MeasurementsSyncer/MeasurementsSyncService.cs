using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer.Persistence;
using Measurements.V1;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncService
{
    private readonly ISyncState syncState;
    private readonly Measurements.V1.Measurements.MeasurementsClient measurementsClient;
    private readonly ILogger<MeasurementsSyncService> logger;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger, ISyncState syncState, Measurements.V1.Measurements.MeasurementsClient measurementsClient)
    {
        this.logger = logger;
        this.syncState = syncState;
        this.measurementsClient = measurementsClient;
    }

    public async Task<List<Measurement>> FetchMeasurements(MeteringPointSyncInfo syncInfo,
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
                var request = new GetMeasurementsRequest
                {
                    DateFrom = dateFrom.Value,
                    DateTo = nearestHour,
                    Gsrn = syncInfo.GSRN,
                    Subject = syncInfo.MeteringPointOwner,
                    Actor = Guid.NewGuid().ToString()
                };
                var res = await measurementsClient.GetMeasurementsAsync(request, cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                    res.Measurements.Count,
                    syncInfo.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(dateFrom.Value).ToString("o"),
                    DateTimeOffset.FromUnixTimeSeconds(nearestHour).ToString("o"));

                if (res.Measurements.Any())
                {
                    var nextSyncPosition = res.Measurements.Max(m => m.DateTo);
                    await syncState.SetSyncPosition(syncInfo.GSRN, nextSyncPosition);
                }

                return res.Measurements.ToList();
            }
            catch (Exception e)
            {
                logger.LogError("An error occured: {error}, no measurements were fetched", e.Message);
            }
        }

        return new();
    }
}
