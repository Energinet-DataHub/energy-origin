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
        IMeasurementSyncMetrics measurementSyncMetrics, IMeasurementSyncPublisher measurementSyncPublisher, Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient, IOptions<MeasurementsSyncOptions> _measurementSyncOptions)
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

    public async Task FetchAndPublishMeasurements(MeteringPointSyncInfo syncInfo, MeteringPointTimeSeriesSlidingWindow slidingWindow,
        CancellationToken stoppingToken)
    {
        var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour();
        var fetchToTimestamp = UnixTimestamp.Min(synchronizationPoint, synchronizationPoint.Add(TimeSpan.FromHours(-_options.Value.MinimumAgeThresholdHours)).RoundToLatestHour());
        if (_options.Value.MinimumAgeThresholdHours > 0)
        {
            synchronizationPoint = UnixTimestamp.Now().Add(TimeSpan.FromHours(-_options.Value.MinimumAgeThresholdHours)).RoundToLatestHour();
        }

        var fetchedMeasurements = await FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, fetchToTimestamp, stoppingToken);
        var meteringPoints = await _meteringPointsClient.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest() { Subject = syncInfo.MeteringPointOwner });
        var meteringPoint = meteringPoints.MeteringPoints.First(mp => mp.MeteringPointId == slidingWindow.GSRN);

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

            _slidingWindowService.UpdateSlidingWindow(slidingWindow, fetchedMeasurements, synchronizationPoint);
            await _slidingWindowState.UpsertSlidingWindow(slidingWindow, stoppingToken);
            await _slidingWindowState.SaveChangesAsync(stoppingToken);
        }
    }

    public async Task<List<Measurement>> FetchMeasurements(MeteringPointTimeSeriesSlidingWindow slidingWindow, string meteringPointOwner,
        UnixTimestamp synchronizationPoint, CancellationToken cancellationToken)
    {
        var dateFrom = slidingWindow.GetFetchIntervalStart().Seconds;
        var synchronizationPointSeconds = synchronizationPoint.Seconds;

        if (dateFrom < synchronizationPointSeconds)
        {
            var request = new GetMeasurementsRequest
            {
                DateFrom = dateFrom,
                DateTo = synchronizationPointSeconds,
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
                DateTimeOffset.FromUnixTimeSeconds(synchronizationPointSeconds).ToString("o"));

            return res.Measurements.ToList();
        }

        return new();
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
            _logger.LogError(e, "An error occured: {error}, no measurements were published, MeteringPoint: {gsrn}", e.Message, syncInfo.Gsrn);
        }
    }
}
