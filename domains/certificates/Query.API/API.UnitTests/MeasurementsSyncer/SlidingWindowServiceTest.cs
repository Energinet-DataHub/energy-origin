using System;
using System.Collections.Generic;
using System.Linq;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Measurements.V1;
using NSubstitute;
using Testing.Helpers;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class SlidingWindowServiceTest
{
    private readonly SlidingWindowService sut;
    private readonly Gsrn GSRN = new Gsrn(GsrnHelper.GenerateRandom());
    private readonly UnixTimestamp now = UnixTimestamp.Now();

    public SlidingWindowServiceTest()
    {
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        sut = new SlidingWindowService(measurementSyncMetrics);
    }

    [Fact]
    public void GivenSynchronizationPoint_WhenCreatingSlidingWindow_NextFetchIntervalStartsAtSynchronizationPoint()
    {
        // Metering point synced up until now
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-1));
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Get fetch interval
        var fetchIntervalStart = window.GetFetchIntervalStart();

        // Assert interval starts at synchronization point
        Assert.Equal(synchronizationPoint, fetchIntervalStart);
    }

    [Fact]
    public void GivenMissingMeasurement_WhenCreatingSlidingWindow_FetchIntervalIncludesMissingIntervals()
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-1));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(-3)), synchronizationPoint.Add(TimeSpan.FromHours(-2))) };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Get fetch interval
        var fetchIntervalStart = window.GetFetchIntervalStart();

        // Assert interval starts at missing interval
        Assert.Equal(synchronizationPoint.Add(TimeSpan.FromHours(-3)), fetchIntervalStart);
    }

    [Fact]
    public void GivenMultipleMissingIntervals_WhenCreatingSlidingWindow_FetchIntervalIncludesAllMissingIntervals()
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-1));
        var missingMeasurements = new List<MeasurementInterval>
        {
            MeasurementInterval.Create(now.Add(TimeSpan.FromHours(-3)), now.Add(TimeSpan.FromHours(-2))),
            MeasurementInterval.Create(now.Add(TimeSpan.FromHours(-6)), now.Add(TimeSpan.FromHours(-5)))
        };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Get fetch interval
        var fetchIntervalStart = window.GetFetchIntervalStart();

        // Assert interval starts at earliest missing interval
        Assert.Equal(now.Add(TimeSpan.FromHours(-6)), fetchIntervalStart);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    public void GivenMeasurements_WhenPublishing_NewMeasurementsShouldBePublished(bool quantityMissing, int publishedCount)
    {
        // Metering point synced up until now with a single missing measurement
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-3));
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, quantityMissing, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds,
                10, quantityMissing, EnergyQuantityValueQuality.Measured)
        };
        var measurementsToPublish = sut.FilterMeasurements(window, measurements);

        // Assert all measurements after synchronization point should be published
        Assert.Equal(publishedCount, measurementsToPublish.Count);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void GivenMissingInterval_WhenPublishing_MeasurementsEqualToMissingIntervalShouldBePublished(bool quantityMissing, int publishedCount)
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-3));
        var missingMeasurements = new List<MeasurementInterval>
        {
            MeasurementInterval.Create(now.Add(TimeSpan.FromHours(-5)), now.Add(TimeSpan.FromHours(-4)))
        };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, now.Add(TimeSpan.FromHours(-5)).Seconds, now.Add(TimeSpan.FromHours(-4)).Seconds, 10, quantityMissing, EnergyQuantityValueQuality.Measured)
        };
        var measurementsToPublish = sut.FilterMeasurements(window, measurements);

        // Assert all measurements after synchronization point should be published
        Assert.Equal(publishedCount, measurementsToPublish.Count());
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    public void GivenMissingInterval_WhenPublishing_MeasurementsInsideMissingIntervalShouldBePublished(bool quantityMissing, int publishedCount)
    {
        // Metering point synced up until now with a missing measurement interval
        var synchronizationPoint = now.RoundToLatestHour();
        var missingMeasurements = new List<MeasurementInterval>
        {
            MeasurementInterval.Create(now.Add(TimeSpan.FromHours(-10)), now.Add(TimeSpan.FromHours(-7)))
        };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = Enumerable.Range(1, 15).Select(i =>
                CreateMeasurement(GSRN, now.Add(TimeSpan.FromHours(-i - 1)).Seconds, now.Add(TimeSpan.FromHours(-i)).Seconds, 10, quantityMissing, EnergyQuantityValueQuality.Measured))
            .ToList();

        var measurementsToPublish = sut.FilterMeasurements(window, measurements);

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
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var newSynchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-4));
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Fake fetched measurements
        var measurements = Enumerable.Range(1, 15).Select(i =>
                CreateMeasurement(GSRN, now.Add(TimeSpan.FromHours(-i - 1)).Seconds, now.Add(TimeSpan.FromHours(-i)).Seconds, 10, quantityMissing, EnergyQuantityValueQuality.Measured))
            .ToList();
        sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

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

        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(5)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(5)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(6)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(6)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(7)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(7)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(8)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(8)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(9)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
        };

        sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Equal(3, window.MissingMeasurements.Intervals.Count);
        Assert.Single(window.MissingMeasurements.Intervals.Where(m =>
            Equals(m.From, synchronizationPoint.Add(TimeSpan.FromHours(1))) && Equals(m.To, synchronizationPoint.Add(TimeSpan.FromHours(4)))));
        Assert.Single(window.MissingMeasurements.Intervals.Where(m =>
            Equals(m.From, synchronizationPoint.Add(TimeSpan.FromHours(6))) && Equals(m.To, synchronizationPoint.Add(TimeSpan.FromHours(7)))));
        Assert.Single(window.MissingMeasurements.Intervals.Where(m =>
            Equals(m.From, synchronizationPoint.Add(TimeSpan.FromHours(8))) && Equals(m.To, newSynchronizationPoint)));
    }

    // Fetched: [---------] - = measurements marked as missing
    // Window:  [ missing ]
    [Fact]
    public void GivenOnlyMissingMeasurements_WhenUpdatingSlidingWindow_MissingIntervalsAreUpdated()
    {
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromHours(4));
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(1)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(2)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromHours(3)).Seconds, synchronizationPoint.Add(TimeSpan.FromHours(4)).Seconds, 10, true, EnergyQuantityValueQuality.Measured),
        };
        sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

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
        var synchronizationPoint = now.RoundToLatestMidnight().Add(TimeSpan.FromDays(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromDays(2));
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromDays(1)).Seconds, 10, false, EnergyQuantityValueQuality.Measured)
        };
        sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

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
        var synchronizationPoint = now.RoundToLatestMidnight().Add(TimeSpan.FromDays(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromDays(2));
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromDays(1)).Seconds, newSynchronizationPoint.Seconds, 10, false, EnergyQuantityValueQuality.Measured)
        };
        sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

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
        var synchronizationPoint = now.RoundToLatestMidnight().Add(TimeSpan.FromDays(-1));
        var newSynchronizationPoint = now.RoundToLatestMidnight();
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint);

        // Fake fetched measurements
        var measurements = new List<Measurement>();
        sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint, window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(newSynchronizationPoint, window.MissingMeasurements.Intervals[0].To);
    }

    [Fact]
    public void GivenMissingMeasurementHour_WhenFetchingQuarters_QuarterValuesShouldBePublished()
    {
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, 10, false, EnergyQuantityValueQuality.Calculated),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(120)).Seconds, 10, false, EnergyQuantityValueQuality.Calculated)
        };
        var measurementsToPublish = sut.FilterMeasurements(window, measurements);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Equal(4, measurementsToPublish.Count);
    }

    [Fact]
    public void GivenMeasurementsWithDifferentQuality_FilterReturnsMeasuredAndCalculated()
    {
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, 10, false, EnergyQuantityValueQuality.Estimated),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds, 10, false, EnergyQuantityValueQuality.Calculated),
        };
        var measurementsToPublish = sut.FilterMeasurements(window, measurements);

        measurementsToPublish.Count.Should().Be(2);
    }

    [Fact]
    public void GivenMeasurementsWithDifferentQuantity_FilterReturnsMeasurementsAboveZero()
    {
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(60)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, 0, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(75)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, 10, false, EnergyQuantityValueQuality.Measured),
            CreateMeasurement(GSRN, synchronizationPoint.Add(TimeSpan.FromMinutes(90)).Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(105)).Seconds, uint.MaxValue, false, EnergyQuantityValueQuality.Calculated),
        };
        var measurementsToPublish = sut.FilterMeasurements(window, measurements);

        measurementsToPublish.Count.Should().Be(1);

    }

    [Fact]
    public void GivenMissingMeasurementInQuarterResolution_WhenUpdatingSlidingWindow_QuarterIntervalsAreCreated()
    {
        var synchronizationPoint = now.RoundToLatestHour().Add(TimeSpan.FromHours(-5));
        var newSynchronizationPoint = synchronizationPoint.Add(TimeSpan.FromHours(3));
        var missingMeasurements = new List<MeasurementInterval>
            { MeasurementInterval.Create(synchronizationPoint.Add(TimeSpan.FromHours(1)), synchronizationPoint.Add(TimeSpan.FromHours(2))) };
        var window = sut.CreateSlidingWindow(GSRN, synchronizationPoint, missingMeasurements);

        // Fake fetched measurements
        var measurements = new List<Measurement>
        {
            CreateMeasurement(GSRN, synchronizationPoint.Seconds, synchronizationPoint.Add(TimeSpan.FromMinutes(15)).Seconds, 10, false, EnergyQuantityValueQuality.Measured)
        };
        sut.UpdateSlidingWindow(window, measurements, newSynchronizationPoint);

        // Assert updated sliding window contains 3 missing intervals
        Assert.Single(window.MissingMeasurements.Intervals);
        Assert.Equal(synchronizationPoint.Add(TimeSpan.FromMinutes(15)), window.MissingMeasurements.Intervals[0].From);
        Assert.Equal(newSynchronizationPoint, window.MissingMeasurements.Intervals[0].To);
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
