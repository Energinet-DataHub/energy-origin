using System;
using System.Collections.Generic;
using System.Linq;
using DataContext.ValueObjects;

namespace DataContext.Models;

public class MeteringPointTimeSeriesSlidingWindow
{
    public string GSRN { get; private set; }
    public UnixTimestamp SynchronizationPoint { get; private set; }
    public MissingMeasurements MissingMeasurements { get; private set; }

    private MeteringPointTimeSeriesSlidingWindow()
    {
        GSRN = String.Empty;
        SynchronizationPoint = UnixTimestamp.Empty();
        MissingMeasurements = new MissingMeasurements();
    }

    private MeteringPointTimeSeriesSlidingWindow(string gsrn, UnixTimestamp synchronizationPoint, List<MeasurementInterval> missingMeasurements)
    {
        GSRN = gsrn;
        SynchronizationPoint = synchronizationPoint;
        MissingMeasurements = new MissingMeasurements(missingMeasurements);
    }

    public static MeteringPointTimeSeriesSlidingWindow Create(Gsrn meteringPointGsrn, UnixTimestamp pointInTimeItShouldSyncUpTo)
    {
        return Create(meteringPointGsrn, pointInTimeItShouldSyncUpTo, new List<MeasurementInterval>());
    }

    public static MeteringPointTimeSeriesSlidingWindow Create(Gsrn meteringPointGsrn, UnixTimestamp pointInTimeItShouldSyncUpTo, List<MeasurementInterval> intervalsWithMeasurementsMissing)
    {
        if (intervalsWithMeasurementsMissing.Any(missingInterval => missingInterval.To > pointInTimeItShouldSyncUpTo))
        {
            throw new ArgumentException("A missing interval cannot extend later than the synchronization point.");
        }

        return new MeteringPointTimeSeriesSlidingWindow(meteringPointGsrn.Value, pointInTimeItShouldSyncUpTo, intervalsWithMeasurementsMissing);
    }

    public void UpdateTo(UnixTimestamp to)
    {
        SynchronizationPoint = to;
    }

    public UnixTimestamp GetFetchIntervalStart()
    {
        if (!MissingMeasurements.Intervals.Any())
        {
            return SynchronizationPoint;
        }

        var earliestMissingMeasurement = MissingMeasurements.Intervals.MinBy(m => m.From.Seconds);
        return earliestMissingMeasurement!.From;
    }

    public void UpdateSlidingWindow(UnixTimestamp newSynchronizationPoint, List<MeasurementInterval> missingMeasurements)
    {
        SynchronizationPoint = newSynchronizationPoint;
        MissingMeasurements = new MissingMeasurements(missingMeasurements);
    }
}

public class MissingMeasurements
{
    public List<MeasurementInterval> Intervals { get; private set; }

    public MissingMeasurements()
    {
        Intervals = new List<MeasurementInterval>();
    }

    public MissingMeasurements(List<MeasurementInterval> intervals)
    {
        Intervals = intervals;
    }
}
