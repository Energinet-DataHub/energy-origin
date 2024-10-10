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
    private readonly MeasurementsSyncOptions _options;

    public SlidingWindowServiceTest()
    {
        _options = new MeasurementsSyncOptions();
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
    public void GivenMissingMeasurementYoungerThan7Days_WhenTimeAdvances_MissingIntervalIsUpdatedAndFetchedLater()
    {
        var synchronizationPoint = _now.RoundToLatestHour().Add(TimeSpan.FromDays(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromHours(1));

        var (missingInterval, measurement) = CreateMissingIntervalAndMeasurement(synchronizationPoint, _gsrn, true, EnergyQuantityValueQuality.Measured);

        var window = _sut.CreateSlidingWindow(_gsrn, synchronizationPoint, [missingInterval]);

        _sut.UpdateSlidingWindow(window, [measurement], newSynchronizationPoint);

        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint, window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(newSynchronizationPoint, window.MissingMeasurements.Intervals[0].To);

        measurement.QuantityMissing = false;

        var advancedSyncPoint = synchronizationPoint.Add(TimeSpan.FromHours(1));
        _sut.UpdateSlidingWindow(window, [measurement], advancedSyncPoint);

        Assert.Empty(window.MissingMeasurements.Intervals);
        Assert.Equal( advancedSyncPoint, window.SynchronizationPoint);
    }

    [Fact]
    public void GivenMinimumAgeIncreases_WhenMinimumAgeIsIncreased_MeasurementsAreStillPublishedWhenReady()
    {
        // Arrange
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var newSyncPoint = now;
        var windowsLastSyncedTo = newSyncPoint;

        // Set the minimum age to 168 hours for this test
        _options.MinimumAgeBeforeIssuingInHours = 168;

        var window = _sut.CreateSlidingWindow(_gsrn, windowsLastSyncedTo);

        var measurements = new List<Measurement>
    {
        CreateMeasurement(_gsrn, now.Seconds, now.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    };

        // Act
        _sut.UpdateSlidingWindow(window, measurements, newSyncPoint);

        // Assert
        Assert.Empty(window.MissingMeasurements.Intervals);
        Assert.Equal(windowsLastSyncedTo, window.SynchronizationPoint);
    }

    // -168h          sync (now)
    //                          -72h
    //  Window:         []

    // 168h <----------------[x]--------------------> 72h <------------------------------------> now

    [Fact]
    public void GivenMinimumAgeDecreases_WhenMinimumAgeIsRemoved_OneMissingIntervalRemains()
    {
        // Arrange
        var initialMinAge = 168; // 7 days in hours
        _options.MinimumAgeBeforeIssuingInHours = initialMinAge;

        // Set the sync point to now minus 7 days
        var syncPoint = _now.RoundToLatestHour().Add(TimeSpan.FromDays(-7));

        // Create the missing interval 4 days ago (1 hour missing)
        var missingInterval = MeasurementInterval.Create(syncPoint.Add(TimeSpan.FromHours(76)).RoundToLatestHour(), syncPoint.Add(TimeSpan.FromHours(77)).RoundToLatestHour());
        var window = _sut.CreateSlidingWindow(_gsrn, syncPoint, new List<MeasurementInterval> { missingInterval });

        // Create measurements to cover all other time intervals except the missing one
        var measurements = Enumerable.Range(1, 6 * 24) // 6 days of measurements (hourly)
            .Select(i =>
                CreateMeasurement(_gsrn, syncPoint.Add(TimeSpan.FromHours(i)).Seconds, syncPoint.Add(TimeSpan.FromHours(i + 1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured))
            .ToList();

        // Now remove the minimum age restriction
        _options.MinimumAgeBeforeIssuingInHours = 0;

        // Try to update the sync position to the current timestamp
        _sut.UpdateSlidingWindow(window, measurements, _now.RoundToLatestHour());

        // There should be only 1 missing interval left
        Assert.Single(window.MissingMeasurements.Intervals);

        //fill the missing interval
        measurements.Add(CreateMeasurement(_gsrn, missingInterval.From.Seconds, missingInterval.To.Seconds, 10, false, EnergyQuantityValueQuality.Measured));

        // Try to update the sync position to the current timestamp
        _sut.UpdateSlidingWindow(window, measurements, _now.RoundToLatestHour());

        // Assert no missing intervals
        Assert.Empty(window.MissingMeasurements.Intervals);
    }

    //     [Fact]
    //     public void IncreasingMinimumAgeRequirement_ShouldNotRePublishExistingMeasurements()
    //     {
    //         // Arrange
    //         var initialMinAge = 72; // 72 hours (3 days)
    //         var increasedMinAge = 168; // 168 hours (7 days)
    //         var options = new MeasurementsSyncOptions { MinimumAgeBeforeIssuingInHours = initialMinAge };
    //         var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
    //         var slidingWindowService = new SlidingWindowService(measurementSyncMetrics, new FakeTimeProvider());
    //         var syncPoint = _now.RoundToLatestHour().Add(TimeSpan.FromHours(-initialMinAge - 1)); // Set sync point before initial min age
    //
    //         var window = slidingWindowService.CreateSlidingWindow(_gsrn, syncPoint);
    //
    //         // Simulate initial fetch and publish
    //         var initialMeasurements = new List<Measurement>
    //         {
    //             CreateMeasurement(_gsrn, syncPoint.Seconds, syncPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    //         };
    //         slidingWindowService.UpdateSlidingWindow(window, initialMeasurements, syncPoint.Add(TimeSpan.FromHours(1)));
    //
    //         // Act
    //         // Increase the minimum age requirement
    //         options.MinimumAgeBeforeIssuingInHours = increasedMinAge;
    //
    //         // Attempt to fetch and publish measurements again
    //         var measurementsAfterIncrease = new List<Measurement>
    //         {
    //             CreateMeasurement(_gsrn, syncPoint.Seconds, syncPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    //         };
    //         var measurementsToPublish = slidingWindowService.FilterMeasurements(window, measurementsAfterIncrease);
    //
    //         // Assert
    //         measurementsToPublish.Should().BeEmpty("because measurements already published should not be re-published after increasing min age requirement");
    //     }
    //
    //     [Fact]
    //     public void GivenSyncedUpToToday_WhenAddingAgeRequirement_SynchronizationPointShouldNotMove()
    //     {
    //         var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
    //         var slidingWindowService = new SlidingWindowService(measurementSyncMetrics);
    //
    //         var initialSyncPoint = UnixTimestamp.Now().RoundToLatestHour();
    //         var window = slidingWindowService.CreateSlidingWindow(_gsrn, initialSyncPoint);
    //
    //         slidingWindowService.UpdateSlidingWindow(window, new List<Measurement>(), initialSyncPoint.Add(TimeSpan.FromHours(1)));
    //
    //
    //         // Assert
    //         Assert.Equal(initialSyncPoint.Add(TimeSpan.FromHours(1)), window.SynchronizationPoint);
    //     }
    //
    //
    //
    //     [Fact]
    //     public void RemovingMinimumAgeRequirement_ShouldAdvanceSynchronizationPointAndFetchAllAvailableMeasurements()
    //     {
    //         // Arrange
    //         var initialMinAgeHours = 72; // 3 days
    //         var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
    //         var slidingWindowService = new SlidingWindowService(measurementSyncMetrics, new FakeTimeProvider());
    //
    //         var initialCutoffTime = _now.Add(-TimeSpan.FromHours(initialMinAgeHours)).RoundToLatestHour();
    //
    //         var synchronizationPoint = _now.Add(-TimeSpan.FromDays(10)).RoundToLatestHour();
    //         var window = slidingWindowService.CreateSlidingWindow(_gsrn, synchronizationPoint);
    //
    //         // Simulate fetching measurements up to initial cutoff time
    //         var initialMeasurements = new List<Measurement>
    //         {
    //             CreateMeasurement(_gsrn, synchronizationPoint.Seconds, initialCutoffTime.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    //         };
    //         slidingWindowService.UpdateSlidingWindow(window, initialMeasurements, initialCutoffTime);
    //
    //         // Act
    //         // Remove the minimum age requirement by setting the cutoff time to now
    //         var newCutoffTime = _now.RoundToLatestHour();
    //         var newMeasurements = new List<Measurement>
    //         {
    //             CreateMeasurement(_gsrn, initialCutoffTime.Seconds, newCutoffTime.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    //         };
    //         slidingWindowService.UpdateSlidingWindow(window, newMeasurements, newCutoffTime);
    //
    //         // Assert
    //         // Synchronization point should advance to the new cutoff time (now)
    //         Assert.Equal(newCutoffTime, window.SynchronizationPoint);
    //
    //         // No missing intervals should be present
    //         Assert.Empty(window.MissingMeasurements.Intervals);
    //     }
    //
    //
    //     [Fact]
    //     public void AddingMinimumAgeRequirement_ShouldRestrictSynchronizationPointToNewCutoffTime()
    //     {
    //         // Arrange
    //         var initialTime = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
    //         var fakeTimeProvider = new FakeTimeProvider(initialTime);
    //         var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
    //         var slidingWindowService = new SlidingWindowService(measurementSyncMetrics, fakeTimeProvider);
    //
    //         // Set current time
    //         var now = UnixTimestamp.Create(fakeTimeProvider.GetUtcNow());
    //
    //         // No initial minimum age requirement; cutoff time is now
    //         var initialCutoffTime = now.RoundToLatestHour();
    //
    //         // Set synchronization point 10 days in the past
    //         var synchronizationPoint = now.Add(-TimeSpan.FromDays(10)).RoundToLatestHour();
    //         var window = slidingWindowService.CreateSlidingWindow(_gsrn, synchronizationPoint);
    //
    //         // Simulate fetching all measurements up to now
    //         var initialMeasurements = new List<Measurement>
    //         {
    //             CreateMeasurement(_gsrn, synchronizationPoint.Seconds, initialCutoffTime.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    //         };
    //         slidingWindowService.UpdateSlidingWindow(window, initialMeasurements, initialCutoffTime);
    //
    //         // Act
    //         // Add a minimum age requirement by setting the cutoff time to a past time
    //         var newMinAgeHours = 72; // 3 days
    //         var newCutoffTime = now.Add(-TimeSpan.FromHours(newMinAgeHours)).RoundToLatestHour();
    //         slidingWindowService.UpdateSlidingWindow(window, new List<Measurement>(), newCutoffTime);
    //
    //         // Assert
    //         // Synchronization point should be adjusted back to the new cutoff time
    //         Assert.Equal(newCutoffTime.Seconds, window.SynchronizationPoint.Seconds);
    //
    //         // Missing intervals should reflect the gap between the previous synchronization point and the new cutoff time
    //         Assert.Empty(window.MissingMeasurements.Intervals);
    //     }
    //
    //
    //     [Fact]
    // public void SynchronizationPoint_ShouldAlwaysRespectCurrentMinimumAgeRequirement()
    // {
    //     // Arrange
    //     var measurementSyncMetrics = Substitute.For<IMeasurementSyncMetrics>();
    //     var slidingWindowService = new SlidingWindowService(measurementSyncMetrics, new FakeTimeProvider());
    //
    //     var initialMinAgeHours = 72; // 3 days
    //     var initialCutoffTime = _now.Add(-TimeSpan.FromHours(initialMinAgeHours)).RoundToLatestHour();
    //
    //     var synchronizationPoint = _now.Add(-TimeSpan.FromDays(10)).RoundToLatestHour();
    //     var window = slidingWindowService.CreateSlidingWindow(_gsrn, synchronizationPoint);
    //
    //     // Simulate fetching measurements up to initial cutoff time
    //     var initialMeasurements = new List<Measurement>
    //     {
    //         CreateMeasurement(_gsrn, synchronizationPoint.Seconds, initialCutoffTime.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    //     };
    //     slidingWindowService.UpdateSlidingWindow(window, initialMeasurements, initialCutoffTime);
    //
    //     // Act
    //     // Decrease the minimum age requirement
    //     var decreasedMinAgeHours = 24; // 1 day
    //     var decreasedCutoffTime = _now.Add(-TimeSpan.FromHours(decreasedMinAgeHours)).RoundToLatestHour();
    //
    //     // Simulate fetching new measurements up to the decreased cutoff time
    //     var newMeasurements = new List<Measurement>
    //     {
    //         CreateMeasurement(_gsrn, initialCutoffTime.Seconds, decreasedCutoffTime.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
    //     };
    //     slidingWindowService.UpdateSlidingWindow(window, newMeasurements, decreasedCutoffTime);
    //
    //     // Assert
    //     // Synchronization point should advance to the decreased cutoff time
    //     Assert.Equal(decreasedCutoffTime, window.SynchronizationPoint);
    //
    //     // Act
    //     // Increase the minimum age requirement
    //     var increasedMinAgeHours = 168; // 7 days
    //     var increasedCutoffTime = _now.Add(-TimeSpan.FromHours(increasedMinAgeHours)).RoundToLatestHour();
    //
    //     slidingWindowService.UpdateSlidingWindow(window, new List<Measurement>(), increasedCutoffTime);
    //
    //     // Assert
    //     // Synchronization point should adjust back to the increased cutoff time
    //     Assert.Equal(increasedCutoffTime, window.SynchronizationPoint);
    // }

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

    private (MeasurementInterval, Measurement) CreateMissingIntervalAndMeasurement(UnixTimestamp synchronizationPoint, Gsrn gsrn, bool quantityMissing, EnergyQuantityValueQuality quality)
    {
        var to = synchronizationPoint.Add(TimeSpan.FromHours(1));

        var missingInterval = MeasurementInterval.Create(synchronizationPoint, to);

        var measurement = CreateMeasurement(gsrn, synchronizationPoint.Seconds, to.Seconds, 10, quantityMissing, quality);

        return (missingInterval, measurement);
    }
}
