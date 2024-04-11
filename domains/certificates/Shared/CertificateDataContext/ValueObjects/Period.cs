using System;
using System.Collections.Generic;

namespace DataContext.ValueObjects;

/// <summary>
/// Time period where the energy was produced.
/// </summary>
/// <param name="DateFrom">In unix timestamp (seconds)</param>
/// <param name="DateTo">In unix timestamp (seconds)</param>
public class Period : ValueObject
{
    private Period()
    {

    }

    public long DateFrom { get; private set; }
    public long DateTo { get; private set; }

    public Period(long dateFrom, long dateTo)
    {
        if (dateFrom >= dateTo)
            throw new ArgumentException("DateFrom must be smaller than DateTo");

        DateFrom = dateFrom;
        DateTo = dateTo;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DateFrom;
        yield return DateTo;
    }
}
