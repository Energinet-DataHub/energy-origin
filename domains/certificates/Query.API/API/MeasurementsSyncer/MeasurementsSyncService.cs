using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using API.Models;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncService
{
    private readonly ISlidingWindowState _slidingWindowState;
    private readonly SlidingWindowService _slidingWindowService;
    private readonly IMeasurementSyncMetrics _measurementSyncMetrics;
    private readonly IMeasurementSyncPublisher _measurementSyncPublisher;
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointsClient;
    private readonly ILogger<MeasurementsSyncService> _logger;
    private readonly IOptions<MeasurementsSyncOptions> _options;
    private readonly IDataHub3Client _dataHub3Client;
    private readonly IDataHubFacadeClient _dataHubFacadeClient;
    private readonly IContractState _contractState;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger,
        ISlidingWindowState slidingWindowState,
        SlidingWindowService slidingWindowService,
        IMeasurementSyncMetrics measurementSyncMetrics,
        IMeasurementSyncPublisher measurementSyncPublisher,
        Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient,
        IOptions<MeasurementsSyncOptions> measurementSyncOptions,
        IDataHub3Client dataHub3Client,
        IDataHubFacadeClient dataHubFacadeClient,
        IContractState contractState)
    {
        _logger = logger;
        _slidingWindowState = slidingWindowState;
        _slidingWindowService = slidingWindowService;
        _measurementSyncMetrics = measurementSyncMetrics;
        _measurementSyncPublisher = measurementSyncPublisher;
        _meteringPointsClient = meteringPointsClient;
        _options = measurementSyncOptions;
        _dataHub3Client = dataHub3Client;
        _dataHubFacadeClient = dataHubFacadeClient;
        _contractState = contractState;
    }

    public async Task FetchAndPublishMeasurements(
        MeteringPointSyncInfo syncInfo,
        MeteringPointTimeSeriesSlidingWindow slidingWindow,
        CancellationToken stoppingToken)
    {
        var gsrn = new Gsrn(slidingWindow.GSRN);
        var mpRelations = await _dataHubFacadeClient.ListCustomerRelations(syncInfo.MeteringPointOwner, [gsrn], stoppingToken);

        if (mpRelations == null)
        {
            _logger.LogError("Something went wrong when getting relations for Gsrn: {Gsrn}", slidingWindow.GSRN);
            return;
        }
        foreach (var rejection in mpRelations.Rejections)
        {
            _logger.LogError("Relation rejection detected. Gsrn: {Gsrn}, ErrorCode: {ErrorCode}, ErrorDetailName: {ErrorDetailName}, ErrorDetailValue: {ErrorDetailValue}", rejection.MeteringPointId, rejection.ErrorCode, rejection.ErrorDetailName, rejection.ErrorDetailValue);
        }

        if (!mpRelations.Relations.Any(x => x.IsValidGsrn(gsrn)))
        {
            _logger.LogError("{Gsrn} does not have a relation. Deleting issuing contract and sliding window for this Gsrn.", slidingWindow.GSRN);
            await _contractState.DeleteContractAndSlidingWindow(gsrn);

            return;
        }

        var newSyncPoint = GetNextSyncPoint(syncInfo);

        var ownedMps = await GetOwnedMeteringPoints(syncInfo);

        var fetchedMeasurements = await FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, newSyncPoint, stoppingToken);
        var meteringPoint = ownedMps.MeteringPoints.First(mp => mp.MeteringPointId == slidingWindow.GSRN);
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
            var gsrns = new List<Gsrn> { new(slidingWindow.GSRN) };
            var mp = await _dataHub3Client.GetMeasurements(gsrns, dateFrom, newSyncPoint.EpochSeconds, cancellationToken);

            if (mp == null || mp.Length == 0)
            {
                _logger.LogInformation("No meteringPointData found for GSRN {GSRN} in period from {from} to: {to}", slidingWindow.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(dateFrom).ToString("o"), DateTimeOffset.FromUnixTimeSeconds(newSyncPoint.EpochSeconds).ToString("o"));
                return [];
            }

            var days = mp[0].PointAggregationGroups;
            var measurements = new List<Measurement>();
            foreach (var day in days)
            {
                day.Value.PointAggregations.ForEach(x =>
                    measurements.Add(new Measurement
                    {
                        Gsrn = slidingWindow.GSRN,
                        DateFrom = x.MinObservationTime,
                        DateTo = UnixTimestamp.Create(x.MinObservationTime).AddHours(1).EpochSeconds,
                        Quantity = x.AggregatedQuantity,
                        Quality = x.Quality.ToEnergyQuality(),
                    }));
            }

            // DH3 should not return measurements after newSyncPoint, but just in case
            measurements = measurements.Where(m => m.DateFrom >= dateFrom && m.DateTo <= newSyncPoint.EpochSeconds).ToList();

            _logger.LogInformation(
                "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                measurements.Count,
                slidingWindow.GSRN,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).ToString("o"),
                DateTimeOffset.FromUnixTimeSeconds(newSyncPoint.EpochSeconds).ToString("o"));

            foreach (var measurement in measurements)
            {
                _logger.LogInformation(
                    "Fetched measurement for GSRN {GSRN} in period from {from} to: {to}, quantity {Quantity}, Quality {Quality}, QuantityMissing {QuantityMissing}",
                    slidingWindow.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(measurement.DateFrom).ToString("o"),
                    DateTimeOffset.FromUnixTimeSeconds(measurement.DateTo).ToString("o"),
                    measurement.Quantity,
                    measurement.Quality,
                    measurement.IsQuantityMissing);
            }

            return measurements;
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
