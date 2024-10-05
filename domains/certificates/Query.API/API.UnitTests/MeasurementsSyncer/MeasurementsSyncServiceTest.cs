using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Measurements.V1;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Testing.Extensions;
using Xunit;
using Technology = DataContext.ValueObjects.Technology;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementsSyncServiceTest
{
    private readonly MeteringPointSyncInfo _syncInfo = new(
        Gsrn: Any.Gsrn(),
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointType.Production,
        "DK1",
        Guid.NewGuid(),
        new Technology("T12345", "T54321"));

    private readonly Measurements.V1.Measurements.MeasurementsClient _fakeClient = Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();
    private readonly ILogger<MeasurementsSyncService> _fakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly ISlidingWindowState _fakeSlidingWindowState = Substitute.For<ISlidingWindowState>();
    private readonly IMeasurementSyncPublisher _fakeMeasurementPublisher = Substitute.For<IMeasurementSyncPublisher>();
    private readonly MeasurementsSyncOptions _options = Substitute.For<MeasurementsSyncOptions>();
    private readonly MeasurementsSyncService _service;

    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _fakeMeteringPointsClient =
        Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

    public MeasurementsSyncServiceTest()
    {
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        _service = new MeasurementsSyncService(_fakeLogger, _fakeSlidingWindowState, _fakeClient, new SlidingWindowService(measurementSyncMetrics),
            new MeasurementSyncMetrics(), _fakeMeasurementPublisher, _fakeMeteringPointsClient, Options.Create(_options));
    }

    [Fact]
    public async Task FetchMeasurements_BeforeContractStartDate_NoDataFetched()
    {
        // Given synchronization point
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(Any.Gsrn(), UnixTimestamp.Now().Add(TimeSpan.FromDays(1)));

        // When fetching measurements
        var response = await _service.FetchMeasurements(slidingWindow, _syncInfo.MeteringPointOwner, UnixTimestamp.Now(), CancellationToken.None);

        // Metering point is skipped
        response.Should().BeEmpty();
        _ = _fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_SyncPositionUpdated()
    {
        // Given synchronization point
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Create(_syncInfo.StartSyncDate));

        // When measurement is received
        var dateTo = UnixTimestamp.Now().RoundToLatestHour().Seconds;
        var measurement = Any.Measurement(_syncInfo.Gsrn, slidingWindow.SynchronizationPoint.Seconds, 5);
        var mockedResponse = new GetMeasurementsResponse { Measurements = { measurement } };
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(mockedResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then sliding window is updated
        await _fakeSlidingWindowState.Received(1)
            .UpsertSlidingWindow(Arg.Is<MeteringPointTimeSeriesSlidingWindow>(t => t.SynchronizationPoint.Seconds == dateTo), CancellationToken.None);
        await _fakeSlidingWindowState.Received().SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurementsReceived_SlidingWindowIsNotUpdated()
    {
        // Given synchronization point
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Create(_syncInfo.StartSyncDate));

        // When no measurements fetched
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);

        var mockedResponse = new GetMeasurementsResponse();
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(mockedResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then sliding window is not updated
        await _fakeSlidingWindowState.Received(0).UpsertSlidingWindow(Arg.Any<MeteringPointTimeSeriesSlidingWindow>(), Arg.Any<CancellationToken>());
        await _fakeSlidingWindowState.DidNotReceive().SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_MeasurementEventsArePublished()
    {
        // Given synchronization point
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Create(_syncInfo.StartSyncDate));

        // When 2 measurements where fetched
        var dateFrom = slidingWindow.SynchronizationPoint.Seconds;
        var measurement1 = Any.Measurement(_syncInfo.Gsrn, dateFrom, 5);
        var measurement2 = Any.Measurement(_syncInfo.Gsrn, dateFrom + 3600, 7);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement1, measurement2 } };
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then 2 measurements are published
        await _fakeMeasurementPublisher.Received().PublishIntegrationEvents(Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Is<List<Measurement>>(measurements => measurements.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void FilterMeasurements_OldEnoughMeasurement_IsNotFilteredOut()
    {
        var minimumAgeBeforeIssuingInHours = 168;
        _options.MinimumAgeBeforeIssuingInHours = minimumAgeBeforeIssuingInHours;
        var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
        var slidingWindowService = new SlidingWindowService(measurementSyncMetrics);

        var gsrn = Any.Gsrn();
        var now = UnixTimestamp.Now();
        var threshold = now.Add(-TimeSpan.FromHours(_options.MinimumAgeBeforeIssuingInHours));

        var synchronizationPoint = threshold.Add(-TimeSpan.FromHours(3)); // 171 hours ago
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, synchronizationPoint);

        var dateFrom = threshold.Add(-TimeSpan.FromHours(1)).Seconds; // 169 hours ago
        var dateTo = threshold.Seconds; // 168 hours ago (1 hour after dateFrom)

        var oldEnoughMeasurement = new Measurement
        {
            Gsrn = slidingWindow.GSRN,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Quantity = 100,
            QuantityMissing = false,
            Quality = EnergyQuantityValueQuality.Measured
        };

        var measurements = new List<Measurement> { oldEnoughMeasurement };

        var publishableMeasurements = slidingWindowService.FilterMeasurements(slidingWindow, measurements, _options.MinimumAgeBeforeIssuingInHours);

        publishableMeasurements.Should().Contain(oldEnoughMeasurement);
    }


    [Fact]
    public void FilterMeasurements_TooRecentMeasurement_IsFilteredOut()
    {
        var minimumAgeBeforeIssuingInHours = 168;
        _options.MinimumAgeBeforeIssuingInHours = minimumAgeBeforeIssuingInHours;
        var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
        var slidingWindowService = new SlidingWindowService(measurementSyncMetrics);

        var gsrn = Any.Gsrn();
        var now = UnixTimestamp.Now();
        var threshold = now.Add(-TimeSpan.FromHours(_options.MinimumAgeBeforeIssuingInHours)); // Threshold is 168 hours ago

        // Synchronization point is before the measurement
        var synchronizationPoint = threshold.Add(-TimeSpan.FromHours(3)); // 171 hours ago
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, synchronizationPoint);

        // Measurement is more recent than threshold (167 hours ago to 166 hours ago)
        var dateFrom = threshold.Add(TimeSpan.FromHours(1)).Seconds; // 167 hours ago
        var dateTo = threshold.Add(TimeSpan.FromHours(2)).Seconds; // 166 hours ago (1 hour after dateFrom)

        var recentMeasurement = new Measurement
        {
            Gsrn = slidingWindow.GSRN,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Quantity = 100,
            QuantityMissing = false,
            Quality = EnergyQuantityValueQuality.Measured
        };

        var measurements = new List<Measurement> { recentMeasurement };

        var publishableMeasurements = slidingWindowService.FilterMeasurements(slidingWindow, measurements, _options.MinimumAgeBeforeIssuingInHours);

        publishableMeasurements.Should().BeEmpty()
            .And.NotContain(recentMeasurement);
    }
}
