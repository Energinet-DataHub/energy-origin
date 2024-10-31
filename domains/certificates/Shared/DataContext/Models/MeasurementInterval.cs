using System;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public class MeasurementInterval
{
    public UnixTimestamp From { get; private set; }
    public UnixTimestamp To { get; private set; }

    private MeasurementInterval()
    {
        From = UnixTimestamp.Empty();
        To = UnixTimestamp.Empty();
    }

    private MeasurementInterval(UnixTimestamp from, UnixTimestamp to)
    {
        From = from;
        To = to;
    }

    public static MeasurementInterval Create(UnixTimestamp from, UnixTimestamp to)
    {
        if (to < from)
        {
            throw new ArgumentException($"To [{to}] must be >= [{from}]");
        }

        return new MeasurementInterval(from, to);
    }

    public bool Contains(MeasurementInterval other)
    {
        return other.From >= From && other.To <= To;
    }
}
