using System;
using System.Collections.Generic;
using System.Linq;
using API.Configurations;
using API.MeasurementsSyncer.Metrics;
using DataContext.Models;
using DataContext.ValueObjects;
using Measurements.V1;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer;

public class SlidingWindowService
{
    private readonly IMeasurementSyncMetrics _measurementSyncMetrics;
    private readonly MeasurementsSyncOptions _options;

    public SlidingWindowService(IMeasurementSyncMetrics measurementSyncMetrics, IOptions<MeasurementsSyncOptions> options)
    {
        _measurementSyncMetrics = measurementSyncMetrics;
        _options = options.Value;
    }

    public MeteringPointTimeSeriesSlidingWindow CreateSlidingWindow(Gsrn gsrn, UnixTimestamp synchronizationPoint)
    {
        return MeteringPointTimeSeriesSlidingWindow.Create(gsrn, synchronizationPoint);
    }

    public MeteringPointTimeSeriesSlidingWindow CreateSlidingWindow(Gsrn gsrn, UnixTimestamp synchronizationPoint,
        List<MeasurementInterval> missingMeasurements)
    {
        return MeteringPointTimeSeriesSlidingWindow.Create(gsrn, synchronizationPoint, missingMeasurements);
    }

    public List<Measurement> FilterMeasurements(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements)
    {
        return measurements
            .Where(m => m.Gsrn == window.GSRN)
            .Where(m =>
            {
                if (m.QuantityMissing)
                {
                    _measurementSyncMetrics.AddFilterDueQuantityMissingFlag(1);
                }
                return !m.QuantityMissing;
            })
            .Where(m =>
            {
                var from = UnixTimestamp.Create(m.DateFrom);
                var to = UnixTimestamp.Create(m.DateTo);
                var interval = MeasurementInterval.Create(from, to);

                if (IsAfterSynchronizationPoint(window, from))
                {
                    return true;
                }
                var isIncludedInMissingInterval = IsIncludedInMissingInterval(window, interval);
                if (isIncludedInMissingInterval)
                {
                    _measurementSyncMetrics.AddNumberOfRecoveredMeasurements(1);
                }
                else
                {
                    _measurementSyncMetrics.AddNumberOfDuplicateMeasurements(1);
                }
                return isIncludedInMissingInterval;
            })
            .Where(m =>
            {
                if (m.Quality is EnergyQuantityValueQuality.Measured or EnergyQuantityValueQuality.Calculated)
                    return true;

                _measurementSyncMetrics.AddFilterDueQuality(1);
                return false;
            })
            .Where(m =>
            {
                if (m.Quantity > 0)
                    return true;

                _measurementSyncMetrics.AddFilterDueQuantityTooLow(1);
                return false;
            })
            .Where(m =>
            {
                if (m.Quantity < uint.MaxValue)
                    return true;

                _measurementSyncMetrics.AddFilterDueQuantityTooHigh(1);
                return false;
            })
            .ToList();
    }

    private static bool IsAfterSynchronizationPoint(MeteringPointTimeSeriesSlidingWindow window, UnixTimestamp from)
    {
        return from >= window.SynchronizationPoint;
    }

    private static bool IsIncludedInMissingInterval(MeteringPointTimeSeriesSlidingWindow window, MeasurementInterval interval)
    {
        return window.MissingMeasurements.Intervals.Any(missingInterval => missingInterval.Contains(interval));
    }

    public void UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements, UnixTimestamp pointInTimeItShouldSyncUpTo)
    {
        // Prevent moving the synchronization point backward when age restriction is applied.
        // If the current synchronization point is ahead of the age threshold,
        // keep it as is to avoid regressing synchronization progress.
        if (window.SynchronizationPoint > pointInTimeItShouldSyncUpTo)
        {
            pointInTimeItShouldSyncUpTo = window.SynchronizationPoint;
        }

        if (NoMeasurementsFetched(measurements))
        {
            var interval = MeasurementInterval.Create(window.SynchronizationPoint, pointInTimeItShouldSyncUpTo);

            if (interval.From < interval.To)
            {
                UpdateMissingMeasurementMetric(new List<MeasurementInterval> { interval });

                var updatedMissingIntervals = window.MissingMeasurements.Intervals.ToList();
                updatedMissingIntervals.Add(interval);

                window.UpdateSlidingWindow(pointInTimeItShouldSyncUpTo, updatedMissingIntervals);
            }

            return;
        }

        var missingIntervals = FindMissingIntervals(window, measurements, pointInTimeItShouldSyncUpTo);
        UpdateMissingMeasurementMetric(missingIntervals);
        window.UpdateSlidingWindow(pointInTimeItShouldSyncUpTo, missingIntervals);
    }

    private UnixTimestamp CalculateMinimumAgeThreshold()
    {
        return UnixTimestamp.Now().Add(TimeSpan.FromHours(-_options.MinimumAgeThresholdHours)).RoundToLatestHour();
    }

    private void UpdateMissingMeasurementMetric(List<MeasurementInterval> missingIntervals)
    {
        foreach (var missingInterval in missingIntervals)
        {
            var secondsOfMissingInterval = missingInterval.To.Seconds - missingInterval.From.Seconds;
            var numberOfMissingIntervals = secondsOfMissingInterval / UnixTimestamp.SecondsPerHour;

            _measurementSyncMetrics.AddNumberOfMissingMeasurement(numberOfMissingIntervals);
        }
    }

    private List<MeasurementInterval> FindMissingIntervals(
        MeteringPointTimeSeriesSlidingWindow window,
        List<Measurement> measurements,
        UnixTimestamp newSynchronizationPoint)
    {
        var minimumAgeThreshold = CalculateMinimumAgeThreshold();

        var sortedMeasurements = SortMeasurementsChronologically(window, measurements);
        var missingIntervals = new List<MeasurementInterval>();
        UnixTimestamp? currentMissingIntervalStart = null;

        for (var currentMeasurementIndex = -1; currentMeasurementIndex < sortedMeasurements.Count + 1; currentMeasurementIndex++)
        {
            if (IsIndexBeforeFirstMeasurement(currentMeasurementIndex))
            {
                if (ContainsGapBeforeFirstMeasurement(window, sortedMeasurements))
                {
                    currentMissingIntervalStart = window.SynchronizationPoint;
                }
                continue;
            }

            if (IsIndexAfterLastMeasurement(currentMeasurementIndex, sortedMeasurements))
            {
                var lastMeasurement = sortedMeasurements[currentMeasurementIndex - 1];
                if (IsCurrentMeasurementIndexInsideMissingInterval(currentMissingIntervalStart))
                {
                    AddMissingIntervalUpToThreshold(currentMissingIntervalStart!, newSynchronizationPoint, missingIntervals, minimumAgeThreshold);
                }
                else if (ContainsGapAfterLastMeasurement(newSynchronizationPoint, lastMeasurement))
                {
                    AddMissingIntervalUpToThreshold(UnixTimestamp.Create(lastMeasurement.DateTo), newSynchronizationPoint, missingIntervals, minimumAgeThreshold);
                }
                continue;
            }

            var currentMeasurement = sortedMeasurements[currentMeasurementIndex];
            if (IsMeasurementQuantityMissing(currentMeasurement))
            {
                if (!IsCurrentMeasurementIndexInsideMissingInterval(currentMissingIntervalStart))
                {
                    currentMissingIntervalStart = UnixTimestamp.Create(currentMeasurement.DateFrom);
                }
            }
            else
            {
                if (IsCurrentMeasurementIndexInsideMissingInterval(currentMissingIntervalStart))
                {
                    AddMissingIntervalUpToThreshold(currentMissingIntervalStart!, UnixTimestamp.Create(currentMeasurement.DateFrom), missingIntervals, minimumAgeThreshold);
                    currentMissingIntervalStart = null;
                }
            }
        }

        var remainingMissingIntervals = window.MissingMeasurements.Intervals
            .Where(interval => interval.To > minimumAgeThreshold)
            .Select(interval => interval.From < minimumAgeThreshold ? MeasurementInterval.Create(minimumAgeThreshold, interval.To) : interval)
            .ToList();

        missingIntervals.AddRange(remainingMissingIntervals);

        return missingIntervals;
    }

    private static void AddMissingIntervalUpToThreshold(UnixTimestamp intervalStart, UnixTimestamp intervalEnd, List<MeasurementInterval> missingIntervals, UnixTimestamp threshold)
    {
        if (intervalStart >= threshold)
            return;

        var adjustedEnd = intervalEnd > threshold ? threshold : intervalEnd;

        if (intervalStart >= adjustedEnd)
            return;

        var missingMeasurementInterval = MeasurementInterval.Create(intervalStart, adjustedEnd);

        missingIntervals.Add(missingMeasurementInterval);
    }

    private static bool IsMeasurementQuantityMissing(Measurement measurement)
    {
        return measurement.QuantityMissing;
    }

    private static bool ContainsGapAfterLastMeasurement(UnixTimestamp newSynchronizationPoint, Measurement lastMeasurement)
    {
        return lastMeasurement.DateTo < newSynchronizationPoint.Seconds;
    }

    private static bool IsCurrentMeasurementIndexInsideMissingInterval(UnixTimestamp? currentMissingIntervalStart)
    {
        return currentMissingIntervalStart is not null;
    }

    private static bool IsIndexAfterLastMeasurement(int currentMeasurementIndex, List<Measurement> sortedMeasurements)
    {
        return currentMeasurementIndex > sortedMeasurements.Count - 1;
    }

    private static bool IsIndexBeforeFirstMeasurement(int currentMeasurementIndex)
    {
        return currentMeasurementIndex == -1;
    }

    private static bool ContainsGapBeforeFirstMeasurement(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> sortedMeasurements)
    {
        return sortedMeasurements[0].DateFrom > window.SynchronizationPoint.Seconds;
    }

    private static List<Measurement> SortMeasurementsChronologically(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements)
    {
        return measurements
            .Where(m => m.Gsrn == window.GSRN)
            .OrderBy(m => m.DateFrom)
            .ToList();
    }

    private static bool NoMeasurementsFetched(List<Measurement> measurements)
    {
        return measurements.Count == 0;
    }
}
