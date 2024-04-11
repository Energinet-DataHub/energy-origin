using System.Collections.Generic;
using System.Linq;
using DataContext.Models;
using DataContext.ValueObjects;
using Measurements.V1;

namespace API.MeasurementsSyncer;

public class SlidingWindowService
{
    public MeteringPointTimeSeriesSlidingWindow CreateSlidingWindow(string gsrn, UnixTimestamp synchronizationPoint)
    {
        return MeteringPointTimeSeriesSlidingWindow.Create(gsrn, synchronizationPoint);
    }

    public MeteringPointTimeSeriesSlidingWindow CreateSlidingWindow(string gsrn, UnixTimestamp synchronizationPoint,
        List<MeasurementInterval> missingMeasurements)
    {
        return MeteringPointTimeSeriesSlidingWindow.Create(gsrn, synchronizationPoint, missingMeasurements);
    }

    public List<Measurement> FilterMeasurements(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements)
    {
        return measurements
            .Where(m => m.Gsrn == window.GSRN)
            .Where(m => !m.QuantityMissing)
            .Where(m =>
            {
                var from = UnixTimestamp.Create(m.DateFrom);
                var to = UnixTimestamp.Create(m.DateTo);
                var interval = MeasurementInterval.Create(from, to);

                if (IsAfterSynchronizationPoint(window, from))
                {
                    return true;
                }

                return IsIncludedInMissingInterval(window, interval);
            }).ToList();
    }

    private static bool IsAfterSynchronizationPoint(MeteringPointTimeSeriesSlidingWindow window, UnixTimestamp from)
    {
        return from >= window.SynchronizationPoint;
    }

    private static bool IsIncludedInMissingInterval(MeteringPointTimeSeriesSlidingWindow window, MeasurementInterval interval)
    {
        return window.MissingMeasurements.Any(missingInterval => missingInterval.Contains(interval));
    }

    public void UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements,
        UnixTimestamp newSynchronizationPoint)
    {
        if (NoMeasurementsFetched(measurements))
        {
            var interval = MeasurementInterval.Create(window.SynchronizationPoint, newSynchronizationPoint);
            window.UpdateSlidingWindow(newSynchronizationPoint, new List<MeasurementInterval> { interval });
            return;
        }

        var missingIntervals = FindMissingIntervals(window, measurements, newSynchronizationPoint);
        window.UpdateSlidingWindow(newSynchronizationPoint, missingIntervals);
    }

    private static List<MeasurementInterval> FindMissingIntervals(MeteringPointTimeSeriesSlidingWindow window, List<Measurement> measurements,
        UnixTimestamp newSynchronizationPoint)
    {
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
                    AddMissingInterval(currentMissingIntervalStart!, newSynchronizationPoint, missingIntervals);
                }
                else if (ContainsGapAfterLastMeasurement(newSynchronizationPoint, lastMeasurement))
                {
                    AddMissingInterval(UnixTimestamp.Create(lastMeasurement.DateTo), newSynchronizationPoint, missingIntervals);
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
                    AddMissingInterval(currentMissingIntervalStart!, UnixTimestamp.Create(currentMeasurement.DateFrom), missingIntervals);
                    currentMissingIntervalStart = null;
                }
            }
        }

        return missingIntervals;
    }

    private static bool IsMeasurementQuantityMissing(Measurement measurement)
    {
        return measurement.QuantityMissing;
    }

    private static bool ContainsGapAfterLastMeasurement(UnixTimestamp newSynchronizationPoint, Measurement lastMeasurement)
    {
        return lastMeasurement.DateTo < newSynchronizationPoint.Seconds;
    }

    private static void AddMissingInterval(UnixTimestamp intervalStart, UnixTimestamp intervalEnd, List<MeasurementInterval> missingIntervals)
    {
        var missingMeasurementInterval = CreateMissingInterval(intervalStart, intervalEnd);
        missingIntervals.Add(missingMeasurementInterval);
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

    private static MeasurementInterval CreateMissingInterval(UnixTimestamp from, UnixTimestamp to)
    {
        var missingMeasurementInterval = MeasurementInterval.Create(from, to);
        return missingMeasurementInterval;
    }
}
