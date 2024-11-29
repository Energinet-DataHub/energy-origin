using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using DocumentFormat.OpenXml.Presentation;
using EnergyOrigin.Domain.ValueObjects;
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
        EndSyncDate: null,
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
        _options.MinimumAgeThresholdHours = 0;
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        _service = new MeasurementsSyncService(_fakeLogger, _fakeSlidingWindowState, _fakeClient,
            new SlidingWindowService(measurementSyncMetrics, Options.Create(_options)),
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
        var dateTo = UnixTimestamp.Now().RoundToLatestHour().EpochSeconds;
        var measurement = Any.Measurement(_syncInfo.Gsrn, slidingWindow.SynchronizationPoint.EpochSeconds, 5);
        var mockedResponse = new GetMeasurementsResponse { Measurements = { measurement } };
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(mockedResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then sliding window is updated
        await _fakeSlidingWindowState.Received(1)
            .UpsertSlidingWindow(Arg.Is<MeteringPointTimeSeriesSlidingWindow>(t => t.SynchronizationPoint.EpochSeconds == dateTo),
                CancellationToken.None);
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
        var dateFrom = slidingWindow.SynchronizationPoint.EpochSeconds;
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
    public async Task AddingAgeRequirementDoesNotFiltersOutMeasurementsWithinMinimumAgeThreshold()
    {
        _options.MinimumAgeThresholdHours = 100;
        var syncPositionFromLastRun = UnixTimestamp.Now().Add(TimeSpan.FromHours(-24)).RoundToLatestHour();
        var missingIntervals = MeasurementInterval.Create(syncPositionFromLastRun.Add(TimeSpan.FromHours(-200)),
            syncPositionFromLastRun.Add(TimeSpan.FromHours(-199)));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPositionFromLastRun, [missingIntervals]);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        var measurementResponse = new GetMeasurementsResponse
        {
            Measurements = { Any.Measurement(_syncInfo.Gsrn, missingIntervals.From.EpochSeconds, 5) }
        };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenMeasurementOutsideMinimumAgeThreshold_WhenCallingFetchAndPublishMeasurements_DoNotPublishMeasurement()
    {
        _options.MinimumAgeThresholdHours = 100;
        var syncPositionFromLastRun = UnixTimestamp.Now().Add(TimeSpan.FromHours(-24)).RoundToLatestHour();
        var missingIntervals = MeasurementInterval.Create(syncPositionFromLastRun.Add(TimeSpan.FromHours(-200)),
            syncPositionFromLastRun.Add(TimeSpan.FromHours(-199)));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPositionFromLastRun, [missingIntervals]);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        var measurementResponse = new GetMeasurementsResponse
        {
            Measurements = { Any.Measurement(_syncInfo.Gsrn, UnixTimestamp.Now().EpochSeconds, 5) }
        };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenMeasurementsWithinAndOutsideThresholdOnlyPublishMeasurementWithinThreshold()
    {
        _options.MinimumAgeThresholdHours = 100;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncPositionFromLastRun = now.Add(TimeSpan.FromHours(-24));
        var missingIntervals = MeasurementInterval.Create(syncPositionFromLastRun.Add(TimeSpan.FromHours(-200)),
            syncPositionFromLastRun.Add(TimeSpan.FromHours(-199)));

        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: missingIntervals.From.ToDateTimeOffset(),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));

        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(
            syncInfo.Gsrn,
            syncPositionFromLastRun,
            [missingIntervals]);

        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        var measurementOutsideThreshold = Any.Measurement(syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-10)).EpochSeconds, 7);
        var measurementWithinThreshold = Any.Measurement(syncInfo.Gsrn, missingIntervals.From.EpochSeconds, 5);

        var measurementResponse = new GetMeasurementsResponse
        {
            Measurements = { measurementOutsideThreshold, measurementWithinThreshold }
        };

        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Is<List<Measurement>>(measurements =>
                measurements.Count == 1 &&
                measurements.Single().DateFrom == measurementWithinThreshold.DateFrom &&
                measurements.Single().DateTo == measurementWithinThreshold.DateTo),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddingAgeRequirement_DoesNotPublishAlreadyPublishedMeasurements()
    {
        _options.MinimumAgeThresholdHours = 2;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindowSyncPoint = now.Add(TimeSpan.FromHours(-4));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, slidingWindowSyncPoint);

        var measurement1 = Any.Measurement(_syncInfo.Gsrn, slidingWindowSyncPoint.EpochSeconds, 5);
        var measurementResponse1 = new GetMeasurementsResponse { Measurements = { measurement1 } };

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse1);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());

        _fakeMeasurementPublisher.ClearReceivedCalls();
        _fakeClient.ClearReceivedCalls();

        _options.MinimumAgeThresholdHours = 20;

        var measurement2 = Any.Measurement(_syncInfo.Gsrn, slidingWindowSyncPoint.AddHours(1).EpochSeconds, 5);
        var measurementResponse2 = new GetMeasurementsResponse { Measurements = { measurement2 } };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse2);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovingMinimumAgeRestriction_AllowsFetchingOfMeasurementsPreviouslyExcludedByHigherAgeRestriction()
    {
        _options.MinimumAgeThresholdHours = 5;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-10)));

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);

        var initialResponse = new GetMeasurementsResponse();
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(initialResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _options.MinimumAgeThresholdHours = 0;
        var measurement = Any.Measurement(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-4)).EpochSeconds, 10);
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

        var syncPoint = now.Add(TimeSpan.FromHours(1));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint);

        var measurement = Any.Measurement(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-2)).EpochSeconds, 10);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement } };
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _options.MinimumAgeThresholdHours = 150;

        var response = await _service.FetchMeasurements(slidingWindow, _syncInfo.MeteringPointOwner, now.Add(TimeSpan.FromHours(-150)),
            CancellationToken.None);

        response.Should().BeEmpty();
    }

    [Fact]
    public async Task DecreasingMinimumAge_AllowsFetchingOfMeasurementsPreviouslyExcludedByHigherAgeRestriction()
    {
        _options.MinimumAgeThresholdHours = 5;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-10)));

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);

        var emptyMeasurementResponse = new GetMeasurementsResponse();
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(emptyMeasurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());

        _fakeClient.ClearReceivedCalls();
        _fakeMeasurementPublisher.ClearReceivedCalls();

        _options.MinimumAgeThresholdHours = 2;
        var measurement = Any.Measurement(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-3)).EpochSeconds, 10);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement } };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingMissingIntervals_AdheresToNewlyAddedAgeRestriction()
    {
        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: DateTimeOffset.Now.AddDays(-14),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));

        var slidingWindow =
            MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, UnixTimestamp.Create(syncInfo.StartSyncDate));

        var dateFrom = slidingWindow.SynchronizationPoint.EpochSeconds;
        var measurement1 = Any.Measurement(syncInfo.Gsrn, dateFrom, 5);
        var measurement2 = Any.Measurement(syncInfo.Gsrn, dateFrom - TimeSpan.FromHours(200).Seconds, 7);
        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurement1, measurement2 } };
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);

        _options.MinimumAgeThresholdHours = 168;

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Do<List<Measurement>>(measurements =>
            {
                measurements.Should().HaveCount(1);
                measurements[0].Should().BeEquivalentTo(measurement2);
            }),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingMeasurementsOutsideOfAgeThreshold_AreNotProcessed()
    {
        _options.MinimumAgeThresholdHours = 96;
        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncPoint = now.Add(TimeSpan.FromDays(-7));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint);

        var missingIntervalOutsideThreshold = MeasurementInterval.Create(now.Add(TimeSpan.FromHours(-1)), now);
        slidingWindow.MissingMeasurements.Intervals.Add(missingIntervalOutsideThreshold);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);

        var measurementResponse = new GetMeasurementsResponse
        {
            Measurements = { Any.Measurement(_syncInfo.Gsrn, missingIntervalOutsideThreshold.From.EpochSeconds, 5) }
        };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenContractAndSlidingWindow_WhenFetchingMeasurements_PointInTimeToSyncToHandlesContractEnd()
    {
        // Given contract and sliding window
        _options.MinimumAgeThresholdHours = 5;
        var latestHour = UnixTimestamp.Now().RoundToLatestHour();
        var contractStart = latestHour.AddHours(-7);
        var contractEnd = latestHour.AddHours(-6);
        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: contractStart.ToDateTimeOffset(),
            EndSyncDate: contractEnd.ToDateTimeOffset(),
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, contractStart);

        // When fetching measurements
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);

        var measurementResponse = new GetMeasurementsResponse();
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        // Then fetch to contract end (and not all the way to now-age)
        var request = (GetMeasurementsRequest)_fakeClient.ReceivedWithAnyArgs(1).ReceivedCalls().First().GetArguments()[0]!;
        request.DateFrom.Should().Be(contractStart.EpochSeconds);
        request.DateTo.Should().Be(contractEnd.EpochSeconds);
    }

    [Fact]
    public async Task GivenContractAndSlidingWindow_WhenFetchingMeasurements_PointInTimeToSyncToHandlesContractWithNoEndDate()
    {
        // Given contract and sliding window
        _options.MinimumAgeThresholdHours = 5;
        var latestHour = UnixTimestamp.Now().RoundToLatestHour();
        var contractStart = latestHour.AddHours(-7);
        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: contractStart.ToDateTimeOffset(),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, contractStart);

        // When fetching measurements
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);

        var measurementResponse = new GetMeasurementsResponse();
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        // Then fetch to contract end (and not all the way to now-age)
        var request = (GetMeasurementsRequest)_fakeClient.ReceivedWithAnyArgs(1).ReceivedCalls().First().GetArguments()[0]!;
        request.DateFrom.Should().Be(contractStart.EpochSeconds);
        request.DateTo.Should().Be(latestHour.AddHours(-_options.MinimumAgeThresholdHours).EpochSeconds);
    }

    [Fact]
    public async Task
        GivenSingleMissingIntervalSpanning7Days_WhenApplying4DayThreshold_OnlyMeasurementsWithinThresholdAreProcessed_AndRemainingDaysAreStillMissing()
    {
        _options.MinimumAgeThresholdHours = 96;
        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncStart = now.Add(TimeSpan.FromDays(-7));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncStart);

        var missingInterval = MeasurementInterval.Create(syncStart, now);
        slidingWindow.MissingMeasurements.Intervals.Add(missingInterval);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>()).Returns(meteringPointsResponse);

        var measurementsWithinThreshold = new List<Measurement>();
        var currentTime = syncStart;
        for (var i = 0; i < 168; i++)
        {
            measurementsWithinThreshold.Add(Any.Measurement(_syncInfo.Gsrn, currentTime.EpochSeconds, 10));
            currentTime = currentTime.Add(TimeSpan.FromHours(1));
        }

        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurementsWithinThreshold } };
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Do<List<Measurement>>(publishedMeasurements =>
            {
                publishedMeasurements.Should().HaveCount(96);
                publishedMeasurements.Should().BeEquivalentTo(measurementsWithinThreshold);
                publishedMeasurements.Should().OnlyContain(m =>
                    m.DateFrom >= now.Add(TimeSpan.FromDays(-4)).EpochSeconds &&
                    m.DateTo <= now.EpochSeconds
                );
            }), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task
        Given_That_We_Have_7days_MissingInterval_And_We_Place_The_AgeThreshold_In_The_Middle_Of_It_Then_Prove_That_Only_MissingIntervals_From_The_SlidingWindows_StartPosition_And_Until_The_AgeThreshold_Are_Being_Processed_Thus_Leaving_The_MissingIntervals_Spanning_From_AgeThresholds_PointInTime_To_CurrentTimeStamp()
    {
        _options.MinimumAgeThresholdHours = 96;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncStart = now.Add(TimeSpan.FromDays(-7)).RoundToLatestHour();

        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: syncStart.ToDateTimeOffset(),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));

        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, syncStart);

        var missingInterval = MeasurementInterval.Create(syncStart, now);
        slidingWindow.MissingMeasurements.Intervals.Add(missingInterval);

        var measurementsWithinThreshold = new List<Measurement>();
        var currentTime = syncStart;
        for (int i = 0; i < 96; i++)
        {
            measurementsWithinThreshold.Add(Any.Measurement(syncInfo.Gsrn, currentTime.EpochSeconds, 10));
            currentTime = currentTime.Add(TimeSpan.FromHours(1));
        }

        var measurementResponse = new GetMeasurementsResponse { Measurements = { measurementsWithinThreshold } };
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(meteringPointsResponse);
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>()).Returns(measurementResponse);

        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Do<List<Measurement>>(publishedMeasurements =>
            {
                publishedMeasurements.Should().HaveCount(96);
                publishedMeasurements.Should().BeEquivalentTo(measurementsWithinThreshold);
                publishedMeasurements.Should().OnlyContain(m =>
                    m.DateFrom >= now.Add(TimeSpan.FromDays(-4)).EpochSeconds &&
                    m.DateTo <= now.EpochSeconds
                );
            }), Arg.Any<CancellationToken>());
    }
}
