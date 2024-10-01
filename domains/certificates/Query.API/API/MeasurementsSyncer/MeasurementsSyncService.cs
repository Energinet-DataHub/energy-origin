using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using Measurements.V1;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncService
{
    private readonly ISlidingWindowState _slidingWindowState;
    private readonly Measurements.V1.Measurements.MeasurementsClient _measurementsClient;
    private readonly SlidingWindowService _slidingWindowService;
    private readonly IMeasurementSyncMetrics _measurementSyncMetrics;
    private readonly IMeasurementSyncPublisher _measurementSyncPublisher;
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointsClient;
    private readonly ILogger<MeasurementsSyncService> _logger;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger, ISlidingWindowState slidingWindowState,
        Measurements.V1.Measurements.MeasurementsClient measurementsClient, SlidingWindowService slidingWindowService,
        IMeasurementSyncMetrics measurementSyncMetrics, IMeasurementSyncPublisher measurementSyncPublisher, Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient)
    {
        _logger = logger;
        _slidingWindowState = slidingWindowState;
        _measurementsClient = measurementsClient;
        _slidingWindowService = slidingWindowService;
        _measurementSyncMetrics = measurementSyncMetrics;
        _measurementSyncPublisher = measurementSyncPublisher;
        _meteringPointsClient = meteringPointsClient;
    }

    public async Task HandleMeteringPoint(MeteringPointSyncInfo syncInfo, CancellationToken stoppingToken)
    {
        try
        {
            var slidingWindow = await _slidingWindowState.GetSlidingWindowStartTime(syncInfo, stoppingToken);
            await FetchAndPublishMeasurements(syncInfo, slidingWindow, stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred: {error}, no measurements were published, MeteringPoint: {gsrn}", e.Message, syncInfo.Gsrn.Value);
        }
    }

public async Task FetchAndPublishMeasurements(MeteringPointSyncInfo syncInfo, MeteringPointTimeSeriesSlidingWindow slidingWindow,
    CancellationToken stoppingToken)
{
    var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour();
    var dateFrom = slidingWindow.GetFetchIntervalStart().Seconds;
    var synchronizationPointSeconds = synchronizationPoint.Seconds;
    var batchSize = (long)TimeSpan.FromHours(1).TotalSeconds;
    var currentDateFrom = dateFrom;

    var meteringPoints = await _meteringPointsClient.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest { Subject = syncInfo.MeteringPointOwner });
    var meteringPoint = meteringPoints.MeteringPoints.First(mp => mp.MeteringPointId == slidingWindow.GSRN);

    var anyMeasurementsProcessed = false;

    while (currentDateFrom < synchronizationPointSeconds)
    {
        var currentDateTo = Math.Min(currentDateFrom + batchSize, synchronizationPointSeconds);

        var measurements = await FetchMeasurementsBatch(slidingWindow.GSRN, syncInfo.MeteringPointOwner, currentDateFrom, currentDateTo, stoppingToken);

        _measurementSyncMetrics.AddNumberOfMeasurementsFetched(measurements.Count);

        if (measurements.Count > 0)
        {
            var measurementsToPublish = _slidingWindowService.FilterMeasurements(slidingWindow, measurements);

            _logger.LogInformation(
                "Publishing {numberOfMeasurementsLeft} of total {numberOfMeasurements} measurements fetched for GSRN {GSRN}",
                measurementsToPublish.Count,
                measurements.Count,
                slidingWindow.GSRN);

            if (measurementsToPublish.Count != 0)
            {
                await _measurementSyncPublisher.PublishIntegrationEvents(meteringPoint, syncInfo, measurementsToPublish, stoppingToken);
            }

            _slidingWindowService.UpdateSlidingWindow(slidingWindow, measurements, synchronizationPoint);

            anyMeasurementsProcessed = true;
        }

        currentDateFrom = currentDateTo;
    }

    // Persist the sliding window if it was updated
    if (anyMeasurementsProcessed)
    {
        await _slidingWindowState.UpsertSlidingWindow(slidingWindow, stoppingToken);
        await _slidingWindowState.SaveChangesAsync(stoppingToken);
    }
}


    private async Task<List<Measurement>> FetchMeasurementsBatch(string gsrn, string meteringPointOwner, long dateFrom, long dateTo, CancellationToken cancellationToken)
    {
        var request = new GetMeasurementsRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Gsrn = gsrn,
            Subject = meteringPointOwner,
            Actor = Guid.NewGuid().ToString()
        };

        var res = await _measurementsClient.GetMeasurementsAsync(request, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Fetched {numberOfMeasurements} measurements for GSRN {GSRN} from {from} to {to}",
            res.Measurements.Count,
            gsrn,
            DateTimeOffset.FromUnixTimeSeconds(dateFrom).ToString("o"),
            DateTimeOffset.FromUnixTimeSeconds(dateTo).ToString("o"));

        return res.Measurements.ToList();
    }
}
