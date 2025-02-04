using System;
using System.Collections.Generic;
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

    public bool Overlaps(MeasurementInterval other)
    {
        return other.Contains(this) || (other.From >= From && other.From <= To) || (other.To >= From && other.To <= To);
    }

    public MeasurementInterval? FindFirstIntervalContaining(List<MeasurementInterval> otherIntervals)
    {
        foreach (var otherInterval in otherIntervals)
        {
            if (otherInterval.Contains(this))
            {
                return otherInterval;
            }
        }
        return null;
    }
}
