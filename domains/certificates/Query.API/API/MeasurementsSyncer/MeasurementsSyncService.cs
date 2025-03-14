using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Measurements.V1;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly IOptions<MeasurementsSyncOptions> _options;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger, ISlidingWindowState slidingWindowState,
        Measurements.V1.Measurements.MeasurementsClient measurementsClient, SlidingWindowService slidingWindowService,
        IMeasurementSyncMetrics measurementSyncMetrics, IMeasurementSyncPublisher measurementSyncPublisher,
        Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient, IOptions<MeasurementsSyncOptions> _measurementSyncOptions)
    {
        _logger = logger;
        _slidingWindowState = slidingWindowState;
        _measurementsClient = measurementsClient;
        _slidingWindowService = slidingWindowService;
        _measurementSyncMetrics = measurementSyncMetrics;
        _measurementSyncPublisher = measurementSyncPublisher;
        _meteringPointsClient = meteringPointsClient;
        _options = _measurementSyncOptions;
    }

    public async Task FetchAndPublishMeasurements(
        MeteringPointSyncInfo syncInfo,
        MeteringPointTimeSeriesSlidingWindow slidingWindow,
        CancellationToken stoppingToken)
    {
        var newSyncPoint = GetNextSyncPoint(syncInfo);

        var fetchedMeasurements = await FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, newSyncPoint, stoppingToken);
        var meteringPoints = await GetOwnedMeteringPoints(syncInfo);
        var meteringPoint = meteringPoints.MeteringPoints.First(mp => mp.MeteringPointId == slidingWindow.GSRN);
        if (meteringPoint.PhysicalStatusOfMp != "E22")
        {
            _logger.LogWarning("Metering point {GSRN} is not in status E22", slidingWindow.GSRN);
        }
        _measurementSyncMetrics.AddNumberOfMeasurementsFetched(fetchedMeasurements.Count);

        if (fetchedMeasurements.Count > 0)
        {
            var measurementsToPublish = _slidingWindowService.FilterMeasurements(slidingWindow, fetchedMeasurements);

            _logger.LogInformation(
                "Publishing {numberOfMeasurementsLeft} of total {numberOfMeasurements} measurements fetched for GSRN {GSRN}",
                measurementsToPublish.Count,
                fetchedMeasurements.Count,
                slidingWindow.GSRN);

            if (measurementsToPublish.Any())
            {
                await _measurementSyncPublisher.PublishIntegrationEvents(meteringPoint, syncInfo, measurementsToPublish, stoppingToken);
            }

            _slidingWindowService.UpdateSlidingWindow(slidingWindow, fetchedMeasurements, newSyncPoint);
            await _slidingWindowState.UpsertSlidingWindow(slidingWindow, stoppingToken);
            await _slidingWindowState.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task<MeteringPointsResponse> GetOwnedMeteringPoints(MeteringPointSyncInfo syncInfo)
    {
        var request = new OwnedMeteringPointsRequest { Subject = syncInfo.MeteringPointOwner };
        return await _meteringPointsClient.GetOwnedMeteringPointsAsync(request);
    }

    private UnixTimestamp GetNextSyncPoint(MeteringPointSyncInfo syncInfo)
    {
        var minimumAgeThresholdInHours = _options.Value.MinimumAgeThresholdHours;

        var latestPossibleSyncTimestamp = UnixTimestamp.Now().Add(TimeSpan.FromHours(-minimumAgeThresholdInHours)).RoundToLatestHour();
        var contractEndSyncTimestamp = syncInfo.EndSyncDate is not null
            ? UnixTimestamp.Create(syncInfo.EndSyncDate.Value)
            : UnixTimestamp.Create(DateTimeOffset.MaxValue);
        var pointInTimeItShouldSyncUpTo = UnixTimestamp.Min(latestPossibleSyncTimestamp, contractEndSyncTimestamp);
        return pointInTimeItShouldSyncUpTo;
    }

    public async Task<List<Measurement>> FetchMeasurements(MeteringPointTimeSeriesSlidingWindow slidingWindow, string meteringPointOwner,
        UnixTimestamp newSyncPoint, CancellationToken cancellationToken)
    {
        var dateFrom = slidingWindow.GetFetchIntervalStart().EpochSeconds;

        if (dateFrom < newSyncPoint.EpochSeconds)
        {
            var request = new GetMeasurementsRequest
            {
                DateFrom = dateFrom,
                DateTo = newSyncPoint.EpochSeconds,
                Gsrn = slidingWindow.GSRN,
                Subject = meteringPointOwner,
                Actor = Guid.NewGuid().ToString()
            };
            var res = await _measurementsClient.GetMeasurementsAsync(request, cancellationToken: cancellationToken);


            _logger.LogInformation(
                "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                res.Measurements.Count,
                slidingWindow.GSRN,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).ToString("o"),
                DateTimeOffset.FromUnixTimeSeconds(newSyncPoint.EpochSeconds).ToString("o"));

            foreach (var measurement in res.Measurements)
            {
                _logger.LogInformation(
                    "Fetched measurement for GSRN {GSRN} in period from {from} to: {to}, quantity {Quantity}, Quality {Quality}, QuantityMissing {QuantityMissing}",
                    slidingWindow.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(measurement.DateFrom).ToString("o"),
                    DateTimeOffset.FromUnixTimeSeconds(measurement.DateTo).ToString("o"),
                    measurement.Quantity,
                    measurement.Quality,
                    measurement.QuantityMissing);
            }

            // DH2 should not return measurements after newSyncPoint, but just in case
            return res.Measurements.Where(m => m.DateTo <= newSyncPoint.EpochSeconds).ToList();
        }

        return new();
    }

    public async Task HandleMeteringPoint(MeteringPointSyncInfo syncInfo, CancellationToken stoppingToken)
    {
        try
        {
            var slidingWindow = await _slidingWindowState.GetSlidingWindowStartTime(syncInfo, stoppingToken);

            _logger.LogInformation("Sliding window for GSRN {GSRN} sync point {SyncPoint}, missing intervals {MissingIntervals}", slidingWindow.GSRN,
                slidingWindow.SynchronizationPoint.ToDateTimeOffset().ToString("o"), slidingWindow.MissingMeasurements);

            await FetchAndPublishMeasurements(syncInfo, slidingWindow, stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured: {error}, no measurements were published, MeteringPoint: {gsrn}", e.Message, syncInfo.Gsrn);
        }
    }
}
