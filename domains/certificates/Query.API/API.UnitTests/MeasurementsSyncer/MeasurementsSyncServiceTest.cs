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
    private readonly MeasurementsSyncService _service;
    private readonly MeasurementsSyncOptions _options = new();
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
    public async Task AddingAgeRequirement_DoesNotFetchAlreadyFetchedMeasurements_UnlessInMissingIntervalWithinMinimumAgeBoundary()
    {
        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncPoint = now.Add(TimeSpan.FromHours(1)).RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint);

        var missingIntervalWhichWillAppear = MeasurementInterval.Create(syncPoint.Add(TimeSpan.FromHours(-200)).RoundToLatestHour(), syncPoint.RoundToLatestHour());
        slidingWindow.MissingMeasurements.Intervals.Add(missingIntervalWhichWillAppear);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        var measurementResponse = new GetMeasurementsResponse
        {
            Measurements = { Any.Measurement(_syncInfo.Gsrn, missingIntervalWhichWillAppear.From.Seconds, 5) }
        };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddingAgeRequirement_DoesNotPublishMeasurementOutsideOfMinimumAgeBoundaryInNextRun()
    {
        _options.MinimumAgeInHours = 150;

        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncPoint = now.Add(TimeSpan.FromHours(1)).RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint);

        var missingIntervalWhichWillAppear = MeasurementInterval.Create(syncPoint.Add(TimeSpan.FromHours(-200)).RoundToLatestHour(), syncPoint.RoundToLatestHour());
        slidingWindow.MissingMeasurements.Intervals.Add(missingIntervalWhichWillAppear);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        var measurementResponse = new GetMeasurementsResponse
        {
            Measurements =
            {
                Any.Measurement(_syncInfo.Gsrn, missingIntervalWhichWillAppear.From.Seconds, 5),
                Any.Measurement(_syncInfo.Gsrn, missingIntervalWhichWillAppear.From.Add(TimeSpan.FromHours(-200)).Seconds, 7)
            }
        };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddingAgeRequirement_DoesNotPublishAlreadyPublishedMeasurements()
    {
        _options.MinimumAgeInHours = 2;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindowSyncPoint = now.Add(TimeSpan.FromHours(-4)).RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, slidingWindowSyncPoint);

        var measurement = Any.Measurement(_syncInfo.Gsrn, slidingWindowSyncPoint.Seconds, 5);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement } };

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());

        _fakeMeasurementPublisher.ClearReceivedCalls();

        _options.MinimumAgeInHours = 20;
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovingMinimumAgeRestriction_AllowsFetchingOfMeasurementsPreviouslyExcludedByHigherAgeRestriction()
    {
        _options.MinimumAgeInHours = 5;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-10)).RoundToLatestHour());

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        var initialResponse = new GetMeasurementsResponse();
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(initialResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _options.MinimumAgeInHours = 0;
        var measurement = Any.Measurement(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-4)).RoundToLatestHour().Seconds, 10);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement } };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyingMinimumAgeRestriction_DoesNotFetchPreviouslyFetchedMeasurementsWithinAgeRange()
    {
        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncPoint = now.Add(TimeSpan.FromHours(1)).RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint);

        var measurement = Any.Measurement(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-2)).RoundToLatestHour().Seconds, 10);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement } };
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _options.MinimumAgeInHours = 150;

        var response = await _service.FetchMeasurements(slidingWindow, _syncInfo.MeteringPointOwner, now.Add(TimeSpan.FromHours(-150)).RoundToLatestHour(), CancellationToken.None);

        response.Should().BeEmpty();
        _ = _fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
    }

    [Fact]
    public async Task DecreasingMinimumAge_AllowsFetchingOfMeasurementsPreviouslyExcludedByHigherAgeRestriction()
    {
        _options.MinimumAgeInHours = 5;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-10)).RoundToLatestHour());

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);

        var emptyMeasurementResponse = new GetMeasurementsResponse();
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(emptyMeasurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _fakeClient.Received(1).GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());

        _fakeClient.ClearReceivedCalls();
        _fakeMeasurementPublisher.ClearReceivedCalls();

        _options.MinimumAgeInHours = 2;
        var measurement = Any.Measurement(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-3)).RoundToLatestHour().Seconds, 10);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement } };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _fakeClient.Received(1).GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }
}
