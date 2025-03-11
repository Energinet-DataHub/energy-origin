using System.Collections.Generic;
using System.Linq;
using API.MeasurementsSyncer.Metrics;
using API.Models;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;

namespace API.MeasurementsSyncer;

public class SlidingWindowService
{
    private readonly IMeasurementSyncMetrics _measurementSyncMetrics;

    public SlidingWindowService(IMeasurementSyncMetrics measurementSyncMetrics)
    {
        _measurementSyncMetrics = measurementSyncMetrics;
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
                if (m.IsQuantityMissing)
                {
                    _measurementSyncMetrics.AddFilterDueQuantityMissingFlag(1);
                }

                return !m.IsQuantityMissing;
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
                if (m.Quality is EnergyQuality.Measured or EnergyQuality.Calculated)
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

    public void UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements, UnixTimestamp newSyncPosition)
    {
        var missingIntervals = FindMissingIntervals(window, measurements, newSyncPosition);
        UpdateMissingMeasurementMetric(missingIntervals);
        var newWindowSyncPosition = UnixTimestamp.Max(window.SynchronizationPoint, newSyncPosition);
        window.UpdateSlidingWindow(newWindowSyncPosition, missingIntervals);
    }

    private void UpdateMissingMeasurementMetric(List<MeasurementInterval> missingIntervals)
    {
        foreach (var missingInterval in missingIntervals)
        {
            var secondsOfMissingInterval = missingInterval.To.EpochSeconds - missingInterval.From.EpochSeconds;
            var numberOfMissingIntervals = secondsOfMissingInterval / UnixTimestamp.SecondsPerHour;

            _measurementSyncMetrics.AddNumberOfMissingMeasurement(numberOfMissingIntervals);
        }
    }

    private List<MeasurementInterval> FindMissingIntervals(
        MeteringPointTimeSeriesSlidingWindow window,
        List<Measurement> measurements,
        UnixTimestamp newSyncPosition)
    {
        // Find new missing intervals in the period from <window.SynchronizationPoint> to <newSyncPosition>
        var newMissingIntervals = FindMissingIntervalsAfterWindowSyncPoint(window, measurements, newSyncPosition);

        // Update missing intervals in the already processed interval up until <window.SynchronizationPoint>
        var updatedMissingIntervals = GetUpdatedMissingIntervalsFromWindow(window, measurements);

        // Return the combined set of missing intervals, overlapping intervals will be joined
        return CombineMissingIntervals(newMissingIntervals, updatedMissingIntervals);
    }

    private List<MeasurementInterval> GetUpdatedMissingIntervalsFromWindow(
        MeteringPointTimeSeriesSlidingWindow window,
        List<Measurement> measurements)
    {
        var measurementsBeforeWindowSyncPoint = measurements.Where(m => m.DateTo <= window.SynchronizationPoint.EpochSeconds).ToList();

        // By default missing intervals are the same as before fetching new measurements
        var updatedMissingIntervals = window.MissingMeasurements.Intervals
            .Select(m => MeasurementInterval.Create(m.From, m.To)).ToList();

        // Loop through each measurement: if measurement is inside a missing interval, then replace the interval with 1..2 new intervals
        foreach (var measurement in measurementsBeforeWindowSyncPoint)
        {
            var measurementInterval = MeasurementInterval.Create(UnixTimestamp.Create(measurement.DateFrom), UnixTimestamp.Create(measurement.DateTo));
            var missingInterval = measurementInterval.FindFirstIntervalContaining(updatedMissingIntervals);
            if (missingInterval is not null)
            {
                updatedMissingIntervals.Remove(missingInterval);

                if (measurementInterval.From > missingInterval.From)
                {
                    updatedMissingIntervals.Add(MeasurementInterval.Create(missingInterval.From, measurementInterval.From));
                }

                if (measurementInterval.To < missingInterval.To)
                {
                    updatedMissingIntervals.Add(MeasurementInterval.Create(measurementInterval.To, missingInterval.To));
                }
            }
        }

        return updatedMissingIntervals;
    }

    private List<MeasurementInterval> FindMissingIntervalsAfterWindowSyncPoint(
        MeteringPointTimeSeriesSlidingWindow window,
        List<Measurement> measurements,
        UnixTimestamp newSyncPosition)
    {
        // If window is already sync'ed to <newSyncPosition> or later, then no missing intervals after <window.SynchronizationPoint> should be added.
        if (window.SynchronizationPoint >= newSyncPosition)
        {
            return [];
        }

        var newSyncStartPosition = window.SynchronizationPoint;
        var measurementsAfterWindowSyncPoint = measurements.Where(m => m.DateFrom >= window.SynchronizationPoint.EpochSeconds).ToList();

        if (measurementsAfterWindowSyncPoint.Count == 0)
        {
            return [MeasurementInterval.Create(window.SynchronizationPoint, newSyncPosition)];
        }

        var sortedMeasurements = SortMeasurementsChronologically(window, measurementsAfterWindowSyncPoint);

        var newMissingIntervals = new List<MeasurementInterval>();
        UnixTimestamp? currentMissingIntervalStart = null;

        for (var currentMeasurementIndex = -1; currentMeasurementIndex < sortedMeasurements.Count + 1; currentMeasurementIndex++)
        {
            if (IsIndexBeforeFirstMeasurement(currentMeasurementIndex))
            {
                if (ContainsGapBeforeFirstMeasurement(newSyncStartPosition, sortedMeasurements))
                {
                    currentMissingIntervalStart = newSyncStartPosition;
                }

                continue;
            }

            if (IsIndexAfterLastMeasurement(currentMeasurementIndex, sortedMeasurements))
            {
                var lastMeasurement = sortedMeasurements[currentMeasurementIndex - 1];
                if (IsCurrentMeasurementIndexInsideMissingInterval(currentMissingIntervalStart))
                {
                    AddMissingIntervalUpToThreshold(currentMissingIntervalStart!, newSyncPosition, newMissingIntervals);
                }
                else if (ContainsGapAfterLastMeasurement(newSyncPosition, lastMeasurement))
                {
                    AddMissingIntervalUpToThreshold(UnixTimestamp.Create(lastMeasurement.DateTo), newSyncPosition, newMissingIntervals);
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
                    AddMissingIntervalUpToThreshold(currentMissingIntervalStart!, UnixTimestamp.Create(currentMeasurement.DateFrom),
                        newMissingIntervals);
                    currentMissingIntervalStart = null;
                }
            }
        }
        return newMissingIntervals;
    }

    private List<MeasurementInterval> CombineMissingIntervals(List<MeasurementInterval> missingIntervals1,
        List<MeasurementInterval> missingIntervals2)
    {
        var missingIntervalsResult = missingIntervals1.Concat(missingIntervals2).ToList();

        var overlappingIntervals = GetOverlappingOrAdjacentIntervals(missingIntervalsResult);
        while (overlappingIntervals is not null)
        {
            var intervalA = overlappingIntervals.Value.a;
            var intervalB = overlappingIntervals.Value.b;
            var combinedInterval = MeasurementInterval.Create(UnixTimestamp.Min(intervalA.From, intervalB.From),
                UnixTimestamp.Max(intervalA.To, intervalB.To));
            missingIntervalsResult.Remove(intervalA);
            missingIntervalsResult.Remove(intervalB);
            missingIntervalsResult.Add(combinedInterval);
            overlappingIntervals = GetOverlappingOrAdjacentIntervals(missingIntervalsResult);
        }

        return missingIntervalsResult;
    }

    private (MeasurementInterval a, MeasurementInterval b)? GetOverlappingOrAdjacentIntervals(List<MeasurementInterval> missingIntervals)
    {
        foreach (var m1 in missingIntervals)
        {
            foreach (var m2 in missingIntervals)
            {
                if (m1 != m2 && (m1.Overlaps(m2) || m1.From == m2.To || m2.From == m1.To))
                {
                    return (m1, m2);
                }
            }
        }
        return null;
    }

    private static void AddMissingIntervalUpToThreshold(UnixTimestamp intervalStart, UnixTimestamp intervalEnd,
        List<MeasurementInterval> missingIntervals)
    {
        var missingMeasurementInterval = MeasurementInterval.Create(intervalStart, intervalEnd);
        missingIntervals.Add(missingMeasurementInterval);
    }

    private static bool IsMeasurementQuantityMissing(Measurement measurement)
    {
        return measurement.IsQuantityMissing;
    }

    private static bool ContainsGapAfterLastMeasurement(UnixTimestamp newSyncEndPosition, Measurement lastMeasurement)
    {
        return lastMeasurement.DateTo < newSyncEndPosition.EpochSeconds;
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

    private static bool ContainsGapBeforeFirstMeasurement(UnixTimestamp newSyncStartPosition, List<Measurement> sortedMeasurements)
    {
        return sortedMeasurements[0].DateFrom > newSyncStartPosition.EpochSeconds;
    }

    private static List<Measurement> SortMeasurementsChronologically(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements)
    {
        return measurements
            .Where(m => m.Gsrn == window.GSRN)
            .OrderBy(m => m.DateFrom)
            .ToList();
    }
}
