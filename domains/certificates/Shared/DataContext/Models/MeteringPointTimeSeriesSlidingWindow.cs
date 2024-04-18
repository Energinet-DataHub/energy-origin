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

    public static MeteringPointTimeSeriesSlidingWindow Create(string gsrn, UnixTimestamp synchronizationPoint)
    {
        return Create(gsrn, synchronizationPoint, new List<MeasurementInterval>());
    }

    public static MeteringPointTimeSeriesSlidingWindow Create(string gsrn, UnixTimestamp synchronizationPoint, List<MeasurementInterval> missingMeasurements)
    {
        return new MeteringPointTimeSeriesSlidingWindow(gsrn, synchronizationPoint, missingMeasurements);
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
    //[NotMapped]
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
