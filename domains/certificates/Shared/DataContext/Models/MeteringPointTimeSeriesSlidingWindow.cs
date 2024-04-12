using System;
using System.Collections.Generic;
using System.Linq;
using DataContext.ValueObjects;

namespace DataContext.Models;

public class MeteringPointTimeSeriesSlidingWindow
{
    public string GSRN { get; private set; }
    public UnixTimestamp SynchronizationPoint { get; private set; }
    public List<MeasurementInterval> MissingMeasurements { get; private set; }

    private MeteringPointTimeSeriesSlidingWindow()
    {
        GSRN = String.Empty;
        SynchronizationPoint = UnixTimestamp.Empty();
        MissingMeasurements = new List<MeasurementInterval>();
    }

    private MeteringPointTimeSeriesSlidingWindow(string gsrn, UnixTimestamp synchronizationPoint, List<MeasurementInterval> missingMeasurements)
    {
        GSRN = gsrn;
        SynchronizationPoint = synchronizationPoint;
        MissingMeasurements = missingMeasurements;
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
        if (!MissingMeasurements.Any())
        {
            return SynchronizationPoint;
        }

        var earliestMissingMeasurement = MissingMeasurements.MinBy(m => m.From.Seconds);
        return earliestMissingMeasurement!.From;
    }

    public void UpdateSlidingWindow(UnixTimestamp newSynchronizationPoint, List<MeasurementInterval> missingMeasurements)
    {
        SynchronizationPoint = newSynchronizationPoint;
        MissingMeasurements = missingMeasurements;
    }
}
