using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using Measurements.V1;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncService(
    ILogger<MeasurementsSyncService> logger,
    ISlidingWindowState slidingWindowState,
    Measurements.V1.Measurements.MeasurementsClient measurementsClient,
    SlidingWindowService slidingWindowService,
    IMeasurementSyncMetrics measurementSyncMetrics,
    IMeasurementSyncPublisher measurementSyncPublisher,
    Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient,
    IOptions<MeasurementsSyncOptions> options)
{
    private readonly MeasurementsSyncOptions _options = options.Value;

    public async Task FetchAndPublishMeasurements(MeteringPointSyncInfo syncInfo, MeteringPointTimeSeriesSlidingWindow slidingWindow,
        CancellationToken stoppingToken)
    {
        var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour();
        var fetchedMeasurements = await FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, synchronizationPoint, stoppingToken);
        var meteringPoints = await meteringPointsClient.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest() { Subject = syncInfo.MeteringPointOwner });
        var meteringPoint = meteringPoints.MeteringPoints.First(mp => mp.MeteringPointId == slidingWindow.GSRN);

        measurementSyncMetrics.AddNumberOfMeasurementsFetched(fetchedMeasurements.Count);

        if (fetchedMeasurements.Count > 0)
        {
            var measurementsToPublish = slidingWindowService.FilterMeasurements(slidingWindow, fetchedMeasurements);

            logger.LogInformation(
                "Publishing {numberOfMeasurementsLeft} of total {numberOfMeasurements} measurements fetched for GSRN {GSRN}",
                measurementsToPublish.Count,
                fetchedMeasurements.Count,
                slidingWindow.GSRN);

            if (measurementsToPublish.Any())
            {
                await measurementSyncPublisher.PublishIntegrationEvents(meteringPoint, syncInfo, measurementsToPublish, stoppingToken);
            }

            slidingWindowService.UpdateSlidingWindow(slidingWindow, fetchedMeasurements, synchronizationPoint);
            await slidingWindowState.UpsertSlidingWindow(slidingWindow, stoppingToken);
            await slidingWindowState.SaveChangesAsync(stoppingToken);
        }
    }

    public async Task<List<Measurement>> FetchMeasurements(MeteringPointTimeSeriesSlidingWindow slidingWindow, string meteringPointOwner,
        UnixTimestamp synchronizationPoint, CancellationToken cancellationToken)
    {
        var dateFrom = slidingWindow.GetFetchIntervalStart().Seconds;
        var synchronizationPointSeconds = synchronizationPoint.Seconds;

        var threshold = UnixTimestamp.Now().Add(-TimeSpan.FromHours(_options.MinimumAgeBeforeIssuingInHours)).Seconds;

        if (dateFrom < synchronizationPointSeconds && synchronizationPointSeconds <= threshold)
        {
            var request = new GetMeasurementsRequest
            {
                DateFrom = dateFrom,
                DateTo = synchronizationPointSeconds,
                Gsrn = slidingWindow.GSRN,
                Subject = meteringPointOwner,
                Actor = Guid.NewGuid().ToString()
            };
            var res = await measurementsClient.GetMeasurementsAsync(request, cancellationToken: cancellationToken);

            logger.LogInformation(
                "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                res.Measurements.Count,
                slidingWindow.GSRN,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).ToString("o"),
                DateTimeOffset.FromUnixTimeSeconds(synchronizationPointSeconds).ToString("o"));

            return res.Measurements.ToList();
        }

        return new();
    }

    public async Task HandleMeteringPoint(MeteringPointSyncInfo syncInfo, CancellationToken stoppingToken)
    {
        try
        {
            var slidingWindow = await slidingWindowState.GetSlidingWindowStartTime(syncInfo, stoppingToken);
            await FetchAndPublishMeasurements(syncInfo, slidingWindow, stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured: {error}, no measurements were published, MeteringPoint: {gsrn}", e.Message, syncInfo.Gsrn);
        }
    }
}
