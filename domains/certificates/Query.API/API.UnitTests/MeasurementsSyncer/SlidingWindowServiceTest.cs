using System;
using System.Collections.Generic;
using System.Linq;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.Models;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class SlidingWindowServiceTest
{
    private readonly SlidingWindowService _sut;
    private readonly Gsrn _gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn();
    private readonly UnixTimestamp _now = UnixTimestamp.Now();
    private readonly IMeasurementSyncMetrics _measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();

    public SlidingWindowServiceTest()
    {
        _sut = new SlidingWindowService(_measurementSyncMetrics);
    }

    [Fact]
    public void GivenSynchronizationPoint_WhenCreatingSlidingWindow_NextFetchIntervalStartsAtSynchronizationPoint()
    {
        // Metering point synced up until now
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-1));
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Get fetch interval
        var fetchIntervalStart = window.GetFetchIntervalStart();

        // Assert interval starts at synchronization point
        Assert.Equal(synchronizationPoint, fetchIntervalStart);
    }

    [Fact]
    public void GivenMissingMeasurement_WhenCreatingSlidingWindow_FetchIntervalIncludesMissingIntervals()
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-1));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(-3)), synchronizationPoint.Add(TimeSpan.FromHours(-2))) };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Get fetch interval
        var fetchIntervalStart = window.GetFetchIntervalStart();

        // Assert interval starts at missing interval
        Assert.Equal(synchronizationPoint.Add(TimeSpan.FromHours(-3)), fetchIntervalStart);
    }

    [Fact]
    public void GivenMultipleMissingIntervals_WhenCreatingSlidingWindow_FetchIntervalIncludesAllMissingIntervals()
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-1));
        var missingMeasurements = new List<MeasurementInterval>
        {
            MeasurementInterval.Create(_now.Add(TimeSpan.FromHours(-3)), _now.Add(TimeSpan.FromHours(-2))),
            MeasurementInterval.Create(_now.Add(TimeSpan.FromHours(-6)), _now.Add(TimeSpan.FromHours(-5)))
        };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Get fetch interval
        var fetchIntervalStart = window.GetFetchIntervalStart();

        // Assert interval starts at earliest missing interval
        Assert.Equal(_now.Add(TimeSpan.FromHours(-6)), fetchIntervalStart);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    public void GivenMeasurements_WhenPublishing_NewMeasurementsShouldBePublished(bool quantityMissing, int publishedCount)
    {
        // Metering point synced up until now with a single missing measurement
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-3));
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.EpochSeconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds, 10,
                quantityMissing ? EnergyQuality.Missing : EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(2)).EpochSeconds,
                10, quantityMissing ? EnergyQuality.Missing : EnergyQuality.Measured)
        };
        var measurementsToPublish = _sut.FilterMeasurements(window, measurements);

        // Assert all measurements after synchronization point should be published
        Assert.Equal(publishedCount, measurementsToPublish.Count);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void GivenMissingInterval_WhenPublishing_MeasurementsEqualToMissingIntervalShouldBePublished(bool quantityMissing, int publishedCount)
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-3));
        var missingMeasurements = new List<MeasurementInterval>
        {
            MeasurementInterval.Create(_now.Add(TimeSpan.FromHours(-5)), _now.Add(TimeSpan.FromHours(-4)))
        };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, _now.Add(TimeSpan.FromHours(-5)).EpochSeconds, _now.Add(TimeSpan.FromHours(-4)).EpochSeconds, 10,
                quantityMissing ? EnergyQuality.Missing : EnergyQuality.Measured)
        };
        var measurementsToPublish = _sut.FilterMeasurements(window, measurements);

        // Assert all measurements after synchronization point should be published
        Assert.Equal(publishedCount, measurementsToPublish.Count());
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    public void GivenMissingInterval_WhenPublishing_MeasurementsInsideMissingIntervalShouldBePublished(bool quantityMissing, int publishedCount)
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = _now.RoundToLatestHour();
        var missingMeasurements = new List<MeasurementInterval>
        {
            MeasurementInterval.Create(_now.Add(TimeSpan.FromHours(-10)), _now.Add(TimeSpan.FromHours(-7)))
        };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = Enumerable.Range(1, 15).Select(i =>
                CreateMeasurement(_gsrn, _now.Add(TimeSpan.FromHours(-i - 1)).EpochSeconds, _now.Add(TimeSpan.FromHours(-i)).EpochSeconds, 10,
                    quantityMissing ? EnergyQuality.Missing : EnergyQuality.Measured))
            .ToList();

        var measurementsToPublish = _sut.FilterMeasurements(window, measurements);

        // Assert all measurements after synchronization point should be published
        Assert.Equal(publishedCount, measurementsToPublish.Count);
    }

    // Fetched: [               ]
    // Synchronization point:   ↑
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenMeasurements_WhenUpdatingSlidingWindow_SynchronizationPointIsUpdated(bool quantityMissing)
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var newSynchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-4));
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = Enumerable.Range(1, 15).Select(i =>
                CreateMeasurement(_gsrn, _now.Add(TimeSpan.FromHours(-i - 1)).EpochSeconds, _now.Add(TimeSpan.FromHours(-i)).EpochSeconds, 10,
                    quantityMissing ? EnergyQuality.Missing : EnergyQuality.Measured))
            .ToList();
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert synchronization point is updated to latest fetched measurement
        Assert.Equal(newSynchronizationPoint, window.SynchronizationPoint);
    }

    // Fetched: [XX------XXXX--XX--] - = measurements marked as missing, X = measurement to publish
    // Window:    [     ]    []  [] Missing intervals
    [Fact]
    public void GivenMissingMeasurements_WhenUpdatingSlidingWindow_MissingIntervalsAreUpdated()
    {
        var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour().Add(TimeSpan.FromHours(-10));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromHours(9));

        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.EpochSeconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds, 10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(2)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(2)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(3)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(3)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(4)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(4)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(5)).EpochSeconds,
                10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(5)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(6)).EpochSeconds,
                10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(6)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(7)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(7)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(8)).EpochSeconds,
                10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(8)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(9)).EpochSeconds,
                10, EnergyQuality.Missing),
        };

        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Equal(3, window.MissingMeasurements.Intervals.Count);
        Assert.Single(window.MissingMeasurements.Intervals, m =>
            Equals(m.From, synchronizationPoint.Add(TimeSpan.FromHours(1))) && Equals(m.To, synchronizationPoint.Add(TimeSpan.FromHours(4))));
        Assert.Single(window.MissingMeasurements.Intervals, m =>
            Equals(m.From, synchronizationPoint.Add(TimeSpan.FromHours(6))) && Equals(m.To, synchronizationPoint.Add(TimeSpan.FromHours(7))));
        Assert.Single(window.MissingMeasurements.Intervals, m =>
            Equals(m.From, synchronizationPoint.Add(TimeSpan.FromHours(8))) && Equals(m.To, newSynchronizationPoint));
    }

    // Fetched: [---------] - = measurements marked as missing
    // Window:  [ missing ]
    [Fact]
    public void GivenOnlyMissingMeasurements_WhenUpdatingSlidingWindow_MissingIntervalsAreUpdated()
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromHours(4));
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.EpochSeconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds, 10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(2)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(2)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(3)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(3)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(4)).EpochSeconds,
                10, EnergyQuality.Missing),
        };
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains a single interval
        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint, window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(newSynchronizationPoint, window.MissingMeasurements.Intervals[0].To);
    }

    // [XXXXXXXXX               ]
    //           [    missing   ]
    [Fact]
    public void GivenLastEntireDayMissingMeasurements_WhenUpdatingSlidingWindow_MissingIntervalsAreUpdated()
    {
        var synchronizationPoint = _now.RoundToLatestMidnight().Add(TimeSpan.FromDays(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromDays(2));
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.EpochSeconds, synchronizationPoint.Add(TimeSpan.FromDays(1)).EpochSeconds, 10, EnergyQuality.Measured)
        };
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 1 missing interval
        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint.Add(TimeSpan.FromDays(1)), window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(newSynchronizationPoint, window.MissingMeasurements.Intervals[0].To);
    }

    // Fetched: [          XXXXXXXXX]
    // Window:  [ missing ]
    [Fact]
    public void GivenFirstEntireDayMissingMeasurements_WhenUpdatingSlidingWindow_MissingIntervalsAreUpdated()
    {
        var synchronizationPoint = _now.RoundToLatestMidnight().Add(TimeSpan.FromDays(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromDays(2));
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromDays(1)).EpochSeconds, newSynchronizationPoint.EpochSeconds, 10, EnergyQuality.Measured)
        };
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint, window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(synchronizationPoint.Add(TimeSpan.FromDays(1)), window.MissingMeasurements.Intervals[0].To);
    }

    // Fetched: [                  ]
    // Window:  [      missing     ]
    [Fact]
    public void GivenNoMeasurementsAtAll_WhenUpdatingSlidingWindow_EntireWindowIsMissing()
    {
        var synchronizationPoint = _now.RoundToLatestMidnight().Add(TimeSpan.FromDays(-1));
        var newSynchronizationPoint = _now.RoundToLatestMidnight();
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>();
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint, window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(newSynchronizationPoint, window.MissingMeasurements.Intervals[0].To);
    }

    [Fact]
    public void GivenMissingMeasurementHour_WhenFetchingQuarters_QuarterValuesShouldBePublished()
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(75)).EpochSeconds, 10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(90)).EpochSeconds, 10, EnergyQuality.Calculated),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(105)).EpochSeconds, 10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(105)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(120)).EpochSeconds, 10, EnergyQuality.Calculated)
        };
        var measurementsToPublish = _sut.FilterMeasurements(window, measurements);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Equal(4, measurementsToPublish.Count);
    }

    [Fact]
    public void GivenMeasurementsWithDifferentQuality_FilterReturnsMeasuredAndCalculated()
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(75)).EpochSeconds, 10, EnergyQuality.Estimated),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(90)).EpochSeconds, 10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(105)).EpochSeconds, 10, EnergyQuality.Calculated),
        };
        var measurementsToPublish = _sut.FilterMeasurements(window, measurements);

        measurementsToPublish.Count.Should().Be(2);
    }

    [Fact]
    public void GivenMeasurementsWithDifferentQuantity_FilterReturnsMeasurementsAboveZero()
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(75)).EpochSeconds, 0, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(90)).EpochSeconds, 10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(105)).EpochSeconds, uint.MaxValue, EnergyQuality.Calculated),
        };
        var measurementsToPublish = _sut.FilterMeasurements(window, measurements);

        measurementsToPublish.Count.Should().Be(1);
    }

    [Fact]
    public void GivenMissingMeasurementInQuarterResolution_WhenUpdatingSlidingWindow_QuarterIntervalsAreCreated()
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromHours(3));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.EpochSeconds, synchronizationPoint.Add(TimeSpan.FromMinutes(15)).EpochSeconds, 10, EnergyQuality.Measured)
        };
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint.Add(TimeSpan.FromMinutes(15)), window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(newSynchronizationPoint, window.MissingMeasurements.Intervals[0].To);
    }

    [Fact]
    public void GivenMissingMeasurements_WhenUpdatingSlidingWindow_MissingMetricIsUpdated()
    {
        var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour().Add(TimeSpan.FromHours(-10));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromHours(9));

        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, synchronizationPoint.EpochSeconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds, 10,
                EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(2)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(2)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(3)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(3)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(4)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(4)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(5)).EpochSeconds,
                10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(5)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(6)).EpochSeconds,
                10,EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(6)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(7)).EpochSeconds,
                10, EnergyQuality.Missing),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(7)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(8)).EpochSeconds,
                10, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(8)).EpochSeconds,
                synchronizationPoint.Add(TimeSpan.FromHours(9)).EpochSeconds,
                10, EnergyQuality.Missing),
        };

        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert metric is updated
        _measurementSyncMetrics.Received(2).AddNumberOfMissingMeasurement(1);
        _measurementSyncMetrics.Received(1).AddNumberOfMissingMeasurement(3);
    }

    [Fact]
    public void GivenAllMeasurementsInWindow_WhenUpdatingSlidingWindow_NoIntervalsAreMissing()
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-10));
        var newSynchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-2));
        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint);

        // Fake fetched measurements
        var measurements = Enumerable.Range(-10, 8).Select(i =>
                CreateMeasurement(_gsrn, _now.RoundToLatestHour().Add(TimeSpan.FromHours(i)).EpochSeconds,
                    _now.RoundToLatestHour().Add(TimeSpan.FromHours(i + 1)).EpochSeconds, 10, EnergyQuality.Measured))
            .ToList();
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert no missing intervals
        Assert.Empty(window.MissingMeasurements.Intervals);
    }

    [Fact]
    public void GivenAgeThreshold_WhenStartingNewSyncInterval_StartingSyncPositionIsNotCreatedBackwardsInTime()
    {
        var initialStartSyncPosition = _now.RoundToLatestHour();

        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, initialStartSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(initialStartSyncPosition);
    }

    [Fact]
    public void GivenNoAgeThreshold_WhenStartingNewSyncInterval_SyncPositionEqualsInitialStartPosition()
    {
        var syncPositionFromLastRun = _now.RoundToLatestHour();

        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, syncPositionFromLastRun);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(syncPositionFromLastRun);
    }

    [Fact]
    public void GivenAgeThreshold_WhenSyncPositionAfterThreshold_SyncPositionIsBlocked()
    {
        var syncPositionFromLastRun = _now.RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, syncPositionFromLastRun);
        var pointInTimeItShouldSyncUpTo = _now.Add(TimeSpan.FromHours(-10));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), pointInTimeItShouldSyncUpTo);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(syncPositionFromLastRun);
    }

    [Fact]
    public void GivenAgeThreshold_WhenSyncPositionMatchingThreshold_SyncPositionIsBlocked()
    {
        var syncPositionFromLastRun = _now.Add(TimeSpan.FromHours(-1)).RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, syncPositionFromLastRun);
        var newSyncPosition = _now.Add(TimeSpan.FromHours(-10));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), newSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(syncPositionFromLastRun);
    }

    [Fact]
    public void GivenAgeThreshold_WhenSyncPositionIsWithinThreshold_SyncPositionMovesForward()
    {
        var syncPositionFromLastRun = _now.Add(TimeSpan.FromHours(-11)).RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, syncPositionFromLastRun);
        var newSyncPosition = syncPositionFromLastRun.Add(TimeSpan.FromHours(1));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), newSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(newSyncPosition);
    }

    [Fact]
    public void GivenNoAgeThreshold_WhenUpdatingSyncPosition_SyncPositionMovementProceedsNormally()
    {
        var syncPositionFromLastRun = _now.RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, syncPositionFromLastRun);
        var newSyncPosition = syncPositionFromLastRun.Add(TimeSpan.FromHours(1));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), newSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(newSyncPosition);
    }

    [Fact]
    public void GivenSlidingWindowWithMissingInterval_WhenSyncingToPointBeforeWindowSyncPoint_ThenWindowSyncPointIsNotMovedButMissingIntervalsAreUpdated()
    {
        // Given sliding window with single missing interval
        var now = _now.RoundToLatestHour();
        var missingIntervalStart = now.Add(TimeSpan.FromHours(-200));
        var missingIntervalEnd = now.Add(TimeSpan.FromHours(-100));
        var syncPoint = now.Add(TimeSpan.FromHours(-50));
        var missingInterval = MeasurementInterval.Create(missingIntervalStart, missingIntervalEnd);
        var window = _sut.CreateSlidingWindow(_gsrn, syncPoint, [missingInterval]);

        var measurements = new List<Measurement>();

        // Measurement in existing missing interval
        var measurement1 = CreateMeasurement(_gsrn, missingIntervalStart.AddHours(1).EpochSeconds, missingIntervalStart.AddHours(2).EpochSeconds, 10,
            EnergyQuality.Measured);
        measurements.Add(measurement1);

        // Measurement before window sync point but outside missing interval
        var measurement2 = CreateMeasurement(_gsrn, missingIntervalEnd.AddHours(3).EpochSeconds, missingIntervalEnd.AddHours(4).EpochSeconds, 11,
            EnergyQuality.Measured);
        measurements.Add(measurement2);

        // New sync position earlier than window sync point
        var newSyncPoint = now.AddHours(-60);
        var filteredMeasurements = _sut.FilterMeasurements(window, measurements);
        _sut.UpdateSlidingWindow(window, measurements, newSyncPoint);

        // Then window sync point is unchanged, two new missing intervals are created and measurement with quantity 10 is published
        filteredMeasurements.Should().ContainSingle(m => m.Quantity == 10);
        window.SynchronizationPoint.Should().Be(syncPoint); // Window sync point not changed
        window.MissingMeasurements.Intervals.Should().HaveCount(2);

        window.MissingMeasurements.Intervals.Should()
            .Contain(m => m.From == missingIntervalStart && m.To == UnixTimestamp.Create(measurement1.DateFrom));

        window.MissingMeasurements.Intervals.Should()
            .Contain(m => m.From == UnixTimestamp.Create(measurement1.DateTo) && m.To == missingIntervalEnd);
    }

    [Fact]
    public void GivenSlidingWindowWithNoMissingIntervals_WhenSyncingToPointBeforeWindowSyncPoint_ThenNoMissingIntervalsAreCreatedAndNoMeasurementsPublished()
    {
        // Given sliding window
        var now = _now.RoundToLatestHour();
        var syncPoint = now.Add(TimeSpan.FromHours(-50));
        var window = _sut.CreateSlidingWindow(_gsrn, syncPoint);

        var measurements = new List<Measurement>();

        // Measurements before window sync point
        var measurement1 = CreateMeasurement(_gsrn, syncPoint.AddHours(-10).EpochSeconds, syncPoint.AddHours(-9).EpochSeconds, 10,
            EnergyQuality.Measured);
        measurements.Add(measurement1);

        var measurement2 = CreateMeasurement(_gsrn, syncPoint.AddHours(-13).EpochSeconds, syncPoint.AddHours(-12).EpochSeconds, 11,
            EnergyQuality.Measured);
        measurements.Add(measurement2);

        // New sync position earlier than window sync point
        var newSyncPoint = syncPoint.AddHours(-1);
        var filteredMeasurements = _sut.FilterMeasurements(window, measurements);
        _sut.UpdateSlidingWindow(window, measurements, newSyncPoint);

        // Then window sync point is unchanged, two new missing intervals are created and measurement with quantity 10 is published
        filteredMeasurements.Should().BeEmpty();
        window.SynchronizationPoint.Should().Be(syncPoint); // Window sync point not changed
        window.MissingMeasurements.Intervals.Should().BeEmpty();
    }

    [Fact]
    public void GivenNoMeasurements_And_AgeThresholdApplied_WhenUpdatingSlidingWindow_ShouldNotSplitTheSlidingWindow()
    {
        var now = _now.RoundToLatestHour();
        var startPositionOfMissingInterval = now.Add(TimeSpan.FromHours(-168));
        var missingInterval = MeasurementInterval.Create(startPositionOfMissingInterval, now);
        var window = _sut.CreateSlidingWindow(_gsrn, now, new List<MeasurementInterval> { missingInterval });

        var measurementsList = new List<Measurement>();

        _sut.UpdateSlidingWindow(window, measurementsList, now.Add(TimeSpan.FromHours(-72)));

        window.MissingMeasurements.Intervals.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                From = startPositionOfMissingInterval,
                To = now
            });
        window.SynchronizationPoint.Should().Be(now);
        _measurementSyncMetrics.Received(0);
    }

    [Fact]
    public void GivenMissingInterval_WhenAddingMeasurementsAcrossThreshold_ShouldSplitMissingIntervalsAndProcessOnlyBeforeThreshold()
    {
        // Given sliding window
        var now = _now.RoundToLatestHour();
        var newSyncPoint = now;

        var missingIntervalStart = now.AddHours(-100);
        var missingIntervalEnd = now.AddHours(-50);
        var syncPoint = missingIntervalEnd;
        var missingInterval = MeasurementInterval.Create(missingIntervalStart, missingIntervalEnd);
        var window = _sut.CreateSlidingWindow(_gsrn, syncPoint, [missingInterval]);

        // When filtering and updating sliding window
        var measurements = new List<Measurement>();
        var beforeThreshold1 = missingIntervalStart.AddHours(10); // After missing interval, before window sync point
        measurements.Add(CreateMeasurement(_gsrn, beforeThreshold1.EpochSeconds, beforeThreshold1.AddHours(1).EpochSeconds, 10, EnergyQuality.Measured));

        var afterThreshold1 = syncPoint.AddHours(2); // After window sync point
        measurements.Add(CreateMeasurement(_gsrn, afterThreshold1.EpochSeconds, afterThreshold1.AddHours(1).EpochSeconds, 10, EnergyQuality.Measured));

        var filteredMeasurements = _sut.FilterMeasurements(window, measurements);
        _sut.UpdateSlidingWindow(window, measurements, newSyncPoint);

        // Then missing intervals before previous sync point are split up and new missing intervals after sync point are created
        window.MissingMeasurements.Intervals.Should().HaveCount(3);

        window.MissingMeasurements.Intervals.Should().Contain(m => m.From == missingIntervalStart && m.To == beforeThreshold1);
        window.MissingMeasurements.Intervals.Should().Contain(m => m.From == beforeThreshold1.AddHours(1) && m.To == afterThreshold1);
        window.MissingMeasurements.Intervals.Should().Contain(m => m.From == afterThreshold1.AddHours(1) && m.To == newSyncPoint);

        // And measurement are not filtered
        filteredMeasurements.Should().HaveCount(2);
    }

    [Fact]
    public void RemovingMinimumAgeRequirement_ShouldAdvanceSynchronizationPointAndFetchAllAvailableMeasurements()
    {
        var initialThreshold = _now.AddHours(-72).RoundToLatestHour();

        var startPositionOfMissingInterval = _now.Add(-TimeSpan.FromDays(10)).RoundToLatestHour();
        var window = _sut.CreateSlidingWindow(_gsrn, startPositionOfMissingInterval);

        var initialMeasurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, startPositionOfMissingInterval.EpochSeconds, initialThreshold.EpochSeconds, 10, EnergyQuality.Measured)
        };
        _sut.UpdateSlidingWindow(window, initialMeasurements, initialThreshold);
        Assert.Equal(initialThreshold, window.SynchronizationPoint);

        var newThreshold = _now.RoundToLatestHour();
        var newMeasurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, initialThreshold.EpochSeconds, newThreshold.EpochSeconds, 10, EnergyQuality.Measured)
        };
        _sut.UpdateSlidingWindow(window, newMeasurements, newThreshold);

        Assert.Equal(newThreshold, window.SynchronizationPoint);

        Assert.Empty(window.MissingMeasurements.Intervals);
    }

    [Fact]
    public void Bug()
    {
        var now = UnixTimestamp.Create(DateTimeOffset.Parse("2024-12-03 02:00:00.664 +01:00")).RoundToLatestHour(); // 2024-12-03 02:00:00.664

        var missingIntervalStart = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-25 05:00:00 +00:00")); // 11/25/2024 05:00:00 +00:00
        var missingIntervalEnd = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-25 23:00:00 +00:00")); // 11/25/2024 23:00:00 +00:00
        var syncPoint = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-26T00:00:00.0000000+00:00"));
        var missingInterval = MeasurementInterval.Create(missingIntervalStart, missingIntervalEnd);
        var window = _sut.CreateSlidingWindow(_gsrn, syncPoint, new List<MeasurementInterval> { missingInterval });

        var measurement1StartTime = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-25T23:00:00.0000000+00:00"));
        var measurement1EndTime = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-26T00:00:00.0000000+00:00"));
        var measurement1Quantity = 70810000;
        var measurement1 = CreateMeasurement(_gsrn, measurement1StartTime.EpochSeconds, measurement1EndTime.EpochSeconds, measurement1Quantity,
            EnergyQuality.Measured);

        var measurement2StartTime = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-26T00:00:00.0000000+00:00"));
        var measurement2EndTime = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-26T01:00:00.0000000+00:00"));
        var measurement2Quantity = 69600000;
        var measurement2 = CreateMeasurement(_gsrn, measurement2StartTime.EpochSeconds, measurement2EndTime.EpochSeconds, measurement2Quantity,
            EnergyQuality.Measured);

        var allMeasurements = new List<Measurement> { measurement1, measurement2 };

        _sut.UpdateSlidingWindow(window, allMeasurements, now.AddHours(-7 * 24));

        window.MissingMeasurements.Intervals.Should().NotBeEmpty();
        var resultInterval = window.MissingMeasurements.Intervals.First();
        resultInterval.From.Should().Be(missingIntervalStart);
        resultInterval.To.Should().Be(missingIntervalEnd);
    }

    [Fact]
    public void GivenSlidingWindow_WhenSyncingUpToPointBeforeWindowSyncPoint_WindowSyncPointIsNotUpdated()
    {
        // Given sliding window
        var now = _now.RoundToLatestHour();
        var window = _sut.CreateSlidingWindow(_gsrn, now.AddHours(-10));

        // When updating sliding window with age restriction and measurement already published
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, now.AddHours(-20).EpochSeconds, now.AddHours(-19).EpochSeconds, 10, EnergyQuality.Measured)
        };

        var filteredMeasurements = _sut.FilterMeasurements(window, measurements);
        _sut.UpdateSlidingWindow(window, measurements, now.AddHours(-18));

        // Then no missing interval is created
        window.MissingMeasurements.Intervals.Should().BeEmpty();

        // And measurement is filtered
        filteredMeasurements.Should().BeEmpty();
    }

    [Fact]
    public void GivenSlidingWindow_WhenSyncingUpToPointAfterWindowSyncPoint_WindowSyncPointIsUpdated()
    {
        // Given sliding window
        var now = _now.RoundToLatestHour();
        var syncPoint = now.AddHours(-10);
        var window = _sut.CreateSlidingWindow(_gsrn, syncPoint);

        // When updating sliding window with age restriction and measurement not published before
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, now.AddHours(-6).EpochSeconds, now.AddHours(-5).EpochSeconds, 10, EnergyQuality.Measured)
        };

        var filteredMeasurements = _sut.FilterMeasurements(window, measurements);
        _sut.UpdateSlidingWindow(window, measurements, now);

        // Then missing intervals are created
        window.MissingMeasurements.Intervals.Should().HaveCount(2);
        window.MissingMeasurements.Intervals.Should().Contain(m => m.From == now.AddHours(-10) && m.To == now.AddHours(-6));
        window.MissingMeasurements.Intervals.Should().Contain(m => m.From == now.AddHours(-5) && m.To == now);

        // And measurement is not filtered
        filteredMeasurements.Should().HaveCount(1);
    }

    [Fact]
    public void GivenSlidingWindow_WhenFetchingMeasurementsAroundSyncPoint_missingIntervalsAreUpdated()
    {
        // Given sliding window
        var now = _now.RoundToLatestHour();
        var syncPoint = now.AddHours(-10);
        var missingInterval = MeasurementInterval.Create(syncPoint.AddHours(-10), syncPoint);
        var window = _sut.CreateSlidingWindow(_gsrn, syncPoint, [missingInterval]);

        // When updating sliding window with age restriction and measurement not published before
        var measurements = new List<Measurement>
        {
            CreateMeasurement(_gsrn, syncPoint.AddHours(-1).EpochSeconds, syncPoint.EpochSeconds, 11, EnergyQuality.Measured),
            CreateMeasurement(_gsrn, syncPoint.EpochSeconds, syncPoint.AddHours(1).EpochSeconds, 22, EnergyQuality.Measured)
        };

        var filteredMeasurements = _sut.FilterMeasurements(window, measurements);
        _sut.UpdateSlidingWindow(window, measurements, now);

        // Then missing intervals are created
        window.MissingMeasurements.Intervals.Should().HaveCount(2);
        window.MissingMeasurements.Intervals.Should().Contain(m => m.From == syncPoint.AddHours(-10) && m.To == syncPoint.AddHours(-1));
        window.MissingMeasurements.Intervals.Should().Contain(m => m.From == syncPoint.AddHours(1) && m.To == now);

        // And measurement is not filtered
        filteredMeasurements.Should().HaveCount(2);
    }

    private Measurement CreateMeasurement(Gsrn gsrn, long from, long to, long quantity, EnergyQuality quality)
    {
        return new Measurement
        {
            Quality = quality,
            DateFrom = from,
            DateTo = to,
            Gsrn = gsrn.Value,
            Quantity = quantity
        };
    }
}
