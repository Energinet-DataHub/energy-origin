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
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Testing.Extensions;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementsSyncerMinimumAgeBeforeIssuanceInHoursTests
{
    private readonly MeteringPointSyncInfo _syncInfo = new(
        Gsrn: Any.Gsrn(),
        StartSyncDate: DateTimeOffset.Now.AddDays(-10), // 10 days in the past
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointType.Production,
        "DK1",
        Guid.NewGuid(),
        new Technology("T12345", "T54321"));

    private readonly Measurements.V1.Measurements.MeasurementsClient _fakeClient =
        Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();

    private readonly ILogger<MeasurementsSyncService> _fakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly ISlidingWindowState _fakeSlidingWindowState = Substitute.For<ISlidingWindowState>();
    private readonly IMeasurementSyncPublisher _fakeMeasurementPublisher = Substitute.For<IMeasurementSyncPublisher>();

    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _fakeMeteringPointsClient =
        Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

    private readonly MeasurementsSyncService _service;

    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider();
    private readonly MeasurementsSyncOptions _options = Substitute.For<MeasurementsSyncOptions>();

    public MeasurementsSyncerMinimumAgeBeforeIssuanceInHoursTests()
    {
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();

        // Create fake metering points
        var ownedMeteringPointsResponse = new MeteringPointsResponse
        {
            MeteringPoints = { Any.MeteringPoint(_syncInfo.Gsrn) }
        };

        // Mock the GetOwnedMeteringPointsAsync to return the fake response
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(GrpcTestHelper.CreateAsyncUnaryCall(ownedMeteringPointsResponse));

        // Initialize the MeasurementsSyncService with the mocked dependencies
        _service = new MeasurementsSyncService(_fakeLogger, _fakeSlidingWindowState, _fakeClient,
            new SlidingWindowService(measurementSyncMetrics), new MeasurementSyncMetrics(),
            _fakeMeasurementPublisher, _fakeMeteringPointsClient, Options.Create(_options));
    }

    [Fact]
    public async Task Case1_SyncPointMovesToNextHole_WhenMeasurementsAreOldEnoughButDoNotMeetQualityOrQuantity()
    {
        // Arrange
        _options.MinimumAgeBeforeIssuingInHours = 168; // 7 days

        var syncPoint = _timeProvider.GetUtcNow(); // Set the synchronization point as current timestamp
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Now());

        // Create fake measurements
        var invalidMeasurements = new List<Measurement>
        {
            Any.Measurement(_syncInfo.Gsrn, syncPoint.AddDays(-8).ToUnixTimeSeconds(), 0, false,
                EnergyQuantityValueQuality.Estimated) // Invalid quality
        };

        // Set up fake client response
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(new GetMeasurementsResponse { Measurements = { invalidMeasurements } });

        // Act
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Assert
        slidingWindow.SynchronizationPoint.Seconds.Should().BeGreaterThan(syncPoint.ToUnixTimeSeconds());
    }



    [Fact]
    public async Task Case2_SyncPointDoesNotMoveUntilMeasurementsAreOldEnough()
    {
        // Arrange
        _options.MinimumAgeBeforeIssuingInHours = 168; // 7 days

        var syncPoint = _timeProvider.GetUtcNow(); // Set the synchronization point as current timestamp
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Now());
        var youngMeasurements = new List<Measurement>
        {
            Any.Measurement(_syncInfo.Gsrn, syncPoint.AddDays(-6).ToUnixTimeSeconds(), 10, false,
                EnergyQuantityValueQuality.Measured)
        };

        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(new GetMeasurementsResponse { Measurements = { youngMeasurements } });

        // Act 1: Run measurement sync
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Assert 1: Sync point should not move
        slidingWindow.SynchronizationPoint.Seconds.Should().Be(syncPoint.ToUnixTimeSeconds());

        // Act 2: Advance time by 2 days to make measurements old enough and run sync again
        _timeProvider.Advance(TimeSpan.FromDays(2));

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Assert 2: Sync point should now move forward
        slidingWindow.SynchronizationPoint.Seconds.Should().BeGreaterThan(syncPoint.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Case3_SyncPointMoves_WhenMeasurementsAreOldEnoughAndMeetAllRequirements()
    {
        // Arrange
        _options.MinimumAgeBeforeIssuingInHours = 168; // 7 days

        var syncPoint = _timeProvider.GetUtcNow(); // Set the synchronization point as current timestamp
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Now());
        var validMeasurements = new List<Measurement>
        {
            Any.Measurement(_syncInfo.Gsrn, syncPoint.AddDays(-9).ToUnixTimeSeconds(), 10, false,
                EnergyQuantityValueQuality.Measured)
        };

        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(new GetMeasurementsResponse { Measurements = { validMeasurements } });

        // Act
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Assert: Sync point should be updated to the next applicable point in time
        slidingWindow.SynchronizationPoint.Seconds.Should().BeGreaterThan(syncPoint.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task SyncPointAndMissingIntervalsBehaveCorrectlyWhenOneIntervalIsOldEnoughAndOneTooYoung()
    {
        // Arrange
        _options.MinimumAgeBeforeIssuingInHours = 168; // 7 days

        // Simulate the sync point starting at the current time
        var syncPoint = _timeProvider.GetUtcNow();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Now());

        // Simulate having two missing intervals: one old enough (10-9 days ago), and one too young (7-6 days ago)
        var missingMeasurementsIntervals = new List<MeasurementInterval>
        {
            MeasurementInterval.Create(
                UnixTimestamp.Create(syncPoint.AddDays(-10).ToUnixTimeSeconds()),
                UnixTimestamp.Create(syncPoint.AddDays(-9).ToUnixTimeSeconds())
            ), // 10-9 days ago (old enough)

            MeasurementInterval.Create(
                UnixTimestamp.Create(syncPoint.AddDays(-7).ToUnixTimeSeconds()),
                UnixTimestamp.Create(syncPoint.AddDays(-6).ToUnixTimeSeconds())
            )  // 7-6 days ago (too young)
        };
        slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Now(), missingMeasurementsIntervals);

        // Create fake measurements for both intervals
        var missingIntervalMeasurements = new List<Measurement>
        {
            Any.Measurement(_syncInfo.Gsrn, syncPoint.AddDays(-10).ToUnixTimeSeconds(), 10, false, EnergyQuantityValueQuality.Measured), // Old enough
            Any.Measurement(_syncInfo.Gsrn, syncPoint.AddDays(-7).ToUnixTimeSeconds(), 10, false, EnergyQuantityValueQuality.Measured)   // Too young
        };

        // Set up the fake client response to return the missing measurements
        _fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(new GetMeasurementsResponse { Measurements = { missingIntervalMeasurements } });

        // Act 1: Run the sync to fetch missing intervals
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Assert 1: Sync point should move forward due to the old enough measurement
        slidingWindow.SynchronizationPoint.Seconds.Should().BeGreaterThan(syncPoint.ToUnixTimeSeconds());

        // Assert 2: The missing interval for the old-enough measurement (10-9 days ago) should be removed
        slidingWindow.MissingMeasurements.Intervals.Should().NotContain(m => m.From.Seconds == UnixTimestamp.Create(syncPoint.AddDays(-10).ToUnixTimeSeconds()).Seconds);

        // Assert 3: The missing interval for the too-young measurement (7-6 days ago) should remain
        slidingWindow.MissingMeasurements.Intervals.Should().Contain(m => m.From.Seconds == UnixTimestamp.Create(syncPoint.AddDays(-7).ToUnixTimeSeconds()).Seconds);
    }
}
