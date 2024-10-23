using System;
using System.Collections.Generic;
using System.Linq;
using API.Configurations;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Measurements.V1;
using Microsoft.Extensions.Options;
using NSubstitute;
using Testing.Helpers;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class SlidingWindowServiceTest
{
    private readonly SlidingWindowService _sut;
    private readonly Gsrn _gsrn = new Gsrn(GsrnHelper.GenerateRandom());
    private readonly UnixTimestamp _now = UnixTimestamp.Now();
    private readonly IMeasurementSyncMetrics _measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
    private readonly MeasurementsSyncOptions _options = new();


    public SlidingWindowServiceTest()
    {
        _sut = new SlidingWindowService(_measurementSyncMetrics, Options.Create(_options));
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
            CreateMeasurement(_gsrn, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, quantityMissing,
                EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds,
                10, quantityMissing, EnergyQuantityValueQuality.Measured)
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
            CreateMeasurement(_gsrn, _now.Add(TimeSpan.FromHours(-5)).Seconds, _now.Add(TimeSpan.FromHours(-4)).Seconds, 10, quantityMissing,
                EnergyQuantityValueQuality.Measured)
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
                CreateMeasurement(_gsrn, _now.Add(TimeSpan.FromHours(-i - 1)).Seconds, _now.Add(TimeSpan.FromHours(-i)).Seconds, 10, quantityMissing,
                    EnergyQuantityValueQuality.Measured))
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
                CreateMeasurement(_gsrn, _now.Add(TimeSpan.FromHours(-i - 1)).Seconds, _now.Add(TimeSpan.FromHours(-i)).Seconds, 10, quantityMissing,
                    EnergyQuantityValueQuality.Measured))
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
            CreateMeasurement(_gsrn, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, false,
                EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(5)).Seconds,
                10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(5)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(6)).Seconds,
                10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(6)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(7)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(7)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(8)).Seconds,
                10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(8)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(9)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
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
            CreateMeasurement(_gsrn, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, true,
                EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
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
            CreateMeasurement(_gsrn, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromDays(1)).Seconds, 10, false,
                EnergyQuantityValueQuality.Measured)
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
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromDays(1)).Seconds, newSynchronizationPoint.Seconds, 10, false,
                EnergyQuantityValueQuality.Measured)
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
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, 10, false, EnergyQuantityValueQuality.Calculated),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(120)).Seconds, 10, false, EnergyQuantityValueQuality.Calculated)
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
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, 10, false, EnergyQuantityValueQuality.Estimated),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds, 10, false, EnergyQuantityValueQuality.Calculated),
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
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, 0, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds,
                synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds, uint.MaxValue, false, EnergyQuantityValueQuality.Calculated),
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
            CreateMeasurement(_gsrn, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(15)).Seconds, 10, false,
                EnergyQuantityValueQuality.Measured)
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
            CreateMeasurement(_gsrn, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, false,
                EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(5)).Seconds,
                10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(5)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(6)).Seconds,
                10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(6)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(7)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(7)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(8)).Seconds,
                10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(_gsrn, synchronizationPoint.Add(TimeSpan.FromHours(8)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(9)).Seconds,
                10, true, EnergyQuantityValueQuality.Measured),
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
                CreateMeasurement(_gsrn, _now.RoundToLatestHour().Add(TimeSpan.FromHours(i)).Seconds,
                    _now.RoundToLatestHour().Add(TimeSpan.FromHours(i + 1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured))
            .ToList();
        _sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert no missing intervals
        Assert.Empty(window.MissingMeasurements.Intervals);
    }

    [Fact]
    public void GivenAgeThreshold_WhenStartingNewSyncInterval_StartingSyncPositionIsNotCreatedBackwardsInTime()
    {
        _options.MinimumAgeThresholdHours = 120;
        var initialStartSyncPosition = _now.RoundToLatestHour();

        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, initialStartSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(initialStartSyncPosition);
    }

    [Fact]
    public void GivenNoAgeThreshold_WhenStartingNewSyncInterval_SyncPositionEqualsInitialStartPosition()
    {
        _options.MinimumAgeThresholdHours = 0;
        var initialStartSyncPosition = _now.RoundToLatestHour();

        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, initialStartSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(initialStartSyncPosition);
    }

    [Fact]
    public void GivenAgeThreshold_WhenSyncPositionAfterThreshold_SyncPositionIsBlocked()
    {
        _options.MinimumAgeThresholdHours = 10;
        var initialStartSyncPosition = _now.Add(TimeSpan.FromHours(-9)).RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, initialStartSyncPosition);
        var newSyncPosition = initialStartSyncPosition.Add(TimeSpan.FromHours(1));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), newSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(initialStartSyncPosition);
    }

    [Fact]
    public void GivenAgeThreshold_WhenSyncPositionMatchingThreshold_SyncPositionIsBlocked()
    {
        _options.MinimumAgeThresholdHours = 10;
        var initialStartSyncPosition = _now.Add(TimeSpan.FromHours(-10)).RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, initialStartSyncPosition);
        var newSyncPosition = initialStartSyncPosition.Add(TimeSpan.FromHours(1));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), newSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(initialStartSyncPosition);
    }

    [Fact]
    public void GivenAgeThreshold_WhenSyncPositionIsWithinThreshold_SyncPositionMovesForward()
    {
        _options.MinimumAgeThresholdHours = 10;
        var initialStartSyncPosition = _now.Add(TimeSpan.FromHours(-11)).RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, initialStartSyncPosition);
        var newSyncPosition = initialStartSyncPosition.Add(TimeSpan.FromHours(1));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), newSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(newSyncPosition);
    }

    [Fact]
    public void GivenNoAgeThreshold_WhenUpdatingSyncPosition_SyncPositionMovementProceedsNormally()
    {
        _options.MinimumAgeThresholdHours = 0;
        var initialStartSyncPosition = _now.RoundToLatestHour();
        var slidingWindow = _sut.CreateSlidingWindow(_gsrn, initialStartSyncPosition);
        var newSyncPosition = initialStartSyncPosition.Add(TimeSpan.FromHours(1));

        _sut.UpdateSlidingWindow(slidingWindow, new List<Measurement>(), newSyncPosition);

        slidingWindow.SynchronizationPoint.Should().BeEquivalentTo(newSyncPosition);
    }



    [Fact]
    public void GivenEntireMissingIntervalWithAgeThresholdApplied_WhenProcessingIntervals_ThenOnlyProcessPartOfIntervalWithinThreshold()
    {
        _options.MinimumAgeThresholdHours = 72;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncStart = now.Add(TimeSpan.FromDays(-7)).RoundToLatestHour();

        var window = _sut.CreateSlidingWindow(_gsrn, syncStart);

        var missingInterval = MeasurementInterval.Create(syncStart, now);
        window.MissingMeasurements.Intervals.Add(missingInterval);

        var allMeasurements = new List<Measurement>();
        var currentTime = syncStart;
        for (int i = 0; i < 168; i++)
        {
            allMeasurements.Add(CreateMeasurement(
                _gsrn,
                currentTime.Seconds,
                currentTime.Add(TimeSpan.FromHours(1)).Seconds,
                10,
                false,
                EnergyQuantityValueQuality.Measured
            ));
            currentTime = currentTime.Add(TimeSpan.FromHours(1));
        }

        _sut.UpdateSlidingWindow(window, allMeasurements, now.Add(TimeSpan.FromHours(-_options.MinimumAgeThresholdHours)));

        window.SynchronizationPoint.Should().Be(syncStart.Add(TimeSpan.FromDays(4)));
        window.MissingMeasurements.Intervals.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                From = syncStart.Add(TimeSpan.FromDays(4)),
                To = now
            });
    }

    [Fact]
    public void GivenMissingIntervalOutsideAgeThreshold_ThenDoesNotProcessIntervalsOutsideThreshold()
    {
        _options.MinimumAgeThresholdHours = 72;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncStart = now.Add(TimeSpan.FromDays(-3)).RoundToLatestHour();

        var window = _sut.CreateSlidingWindow(_gsrn, syncStart);

        var missingInterval = MeasurementInterval.Create(now.Add(TimeSpan.FromDays(-3)), now);
        window.MissingMeasurements.Intervals.Add(missingInterval);

        var allMeasurements = new List<Measurement>();
        var currentTime = syncStart;
        for (int i = 0; i < 72; i++)
        {
            allMeasurements.Add(CreateMeasurement(
                _gsrn,
                currentTime.Seconds,
                currentTime.Add(TimeSpan.FromHours(1)).Seconds,
                10,
                false,
                EnergyQuantityValueQuality.Measured
            ));
            currentTime = currentTime.Add(TimeSpan.FromHours(1));
        }

        _sut.UpdateSlidingWindow(window, allMeasurements, now);

        window.MissingMeasurements.Intervals.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                From = syncStart,
                To = now
            });
        window.SynchronizationPoint.Should().Be(now.Add(TimeSpan.FromDays(-3)));
        _measurementSyncMetrics.Received(0).AddNumberOfMeasurementsPublished(72);
    }

    [Fact]
    public void GivenNoMeasurements_And_AgeThresholdApplied_WhenUpdatingSlidingWindow_ShouldNotSplitTheSlidingWindow()
    {
        _options.MinimumAgeThresholdHours = 72;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncStart = now.Add(TimeSpan.FromDays(-7)).RoundToLatestHour();
        var window = _sut.CreateSlidingWindow(_gsrn, syncStart);
        var missingInterval = MeasurementInterval.Create(syncStart, now);
        window.MissingMeasurements.Intervals.Add(missingInterval);
        var measurementsList = new List<Measurement>();
        var currentTime = syncStart;
        for (int i = 0; i < 168; i++)
        {
            measurementsList.Add(CreateMeasurement(
                _gsrn,
                currentTime.Seconds,
                currentTime.Add(TimeSpan.FromHours(1)).Seconds,
                10,
                false,
                EnergyQuantityValueQuality.Measured
            ));
            currentTime = currentTime.Add(TimeSpan.FromHours(1));
        }

        _sut.UpdateSlidingWindow(window, measurementsList, now);

        window.MissingMeasurements.Intervals.Should().ContainSingle();
        window.SynchronizationPoint.Should().Be(syncStart);
    }

    [Fact]
    public void Given7DayMissingInterval_WhenAddingMeasurementsAcrossThreshold_ShouldSplitMissingIntervalsAndProcessOnlyBeforeThreshold()
    {
        _options.MinimumAgeThresholdHours = 72;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncStart = now.Add(TimeSpan.FromDays(-7)).RoundToLatestHour();

        var window = _sut.CreateSlidingWindow(_gsrn, syncStart);
        var missingInterval = MeasurementInterval.Create(syncStart, now);
        window.MissingMeasurements.Intervals.Add(missingInterval);

        var measurements = new List<Measurement>();

        var beforeThreshold1 = syncStart.Add(TimeSpan.FromHours(10));
        var beforeThreshold2 = syncStart.Add(TimeSpan.FromHours(12));
        measurements.Add(CreateMeasurement(_gsrn, beforeThreshold1.Seconds, beforeThreshold1.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured));
        measurements.Add(CreateMeasurement(_gsrn, beforeThreshold2.Seconds, beforeThreshold2.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured));

        var afterThreshold1 = now.Add(TimeSpan.FromHours(-48));
        var afterThreshold2 = now.Add(TimeSpan.FromHours(-24));
        measurements.Add(CreateMeasurement(_gsrn, afterThreshold1.Seconds, afterThreshold1.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured));
        measurements.Add(CreateMeasurement(_gsrn, afterThreshold2.Seconds, afterThreshold2.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured));

        _sut.UpdateSlidingWindow(window, measurements, now);

        var threshold = now.Add(TimeSpan.FromHours(-_options.MinimumAgeThresholdHours));

        window.MissingMeasurements.Intervals.Should().HaveCount(2);

        window.MissingMeasurements.Intervals[0].Should().BeEquivalentTo(new
        {
            From = syncStart,
            To = beforeThreshold1
        });

        window.MissingMeasurements.Intervals[1].Should().BeEquivalentTo(new
        {
            From = threshold,
            To = now
        });

        _measurementSyncMetrics.Received(1).AddNumberOfMissingMeasurement(10);
        _measurementSyncMetrics.Received(1).AddNumberOfMissingMeasurement(72);
    }

    [Fact]
    public void RemovingMinimumAgeRequirement_ShouldAdvanceSynchronizationPointAndFetchAllAvailableMeasurements()
    {
        var initialMinAgeHours = 72;
        var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
        var slidingWindowService = new SlidingWindowService(measurementSyncMetrics, Options.Create(_options));
        _options.MinimumAgeThresholdHours = 72;

        var initialCutoffTime = _now.Add(-TimeSpan.FromHours(initialMinAgeHours)).RoundToLatestHour();

        var synchronizationPoint = _now.Add(-TimeSpan.FromDays(10)).RoundToLatestHour();
        var window = slidingWindowService.CreateSlidingWindow(_gsrn, synchronizationPoint);

        var initialMeasurements = new List<Measurement>
    {
        CreateMeasurement(_gsrn, synchronizationPoint.Seconds, initialCutoffTime.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    };
        slidingWindowService.UpdateSlidingWindow(window, initialMeasurements, initialCutoffTime);

        _options.MinimumAgeThresholdHours = 0;
        var newCutoffTime = _now.RoundToLatestHour();
        var newMeasurements = new List<Measurement>
    {
        CreateMeasurement(_gsrn, initialCutoffTime.Seconds, newCutoffTime.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    };
        slidingWindowService.UpdateSlidingWindow(window, newMeasurements, newCutoffTime);

        Assert.Equal(newCutoffTime, window.SynchronizationPoint);

        Assert.Empty(window.MissingMeasurements.Intervals);
    }

    private Measurement CreateMeasurement(Gsrn gsrn, long from, long to, long quantity, bool quantityMissing, EnergyQuantityValueQuality quality)
    {
        return new Measurement
        {
            Quality = quality,
            DateFrom = from,
            DateTo = to,
            Gsrn = gsrn.Value,
            Quantity = quantity,
            QuantityMissing = quantityMissing
        };
    }
}
