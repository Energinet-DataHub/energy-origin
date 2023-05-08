using ProjectOrigin.Electricity.Client.Models;

namespace DomainCertificate.ValueObjects;

/// <summary>
/// Time period where the energy was produced.
/// </summary>
/// <param name="DateFrom">In unix timestamp (seconds)</param>
/// <param name="DateTo">In unix timestamp (seconds)</param>
public class Period : ValueObject
{
    public long DateFrom { get; } // EnergyMeasured.DateFrom
    public long DateTo { get; } // EnergyMeasured.DateTo

    public Period(long dateFrom, long dateTo)
    {
        if (dateFrom >= dateTo)
            throw new ArgumentException("DateFrom must be smaller than DateTo");

        DateFrom = dateFrom;
        DateTo = dateTo;
    }

    public DateInterval ToDateInterval() =>
        new(
            new DateTimeOffset(DateFrom * TimeSpan.TicksPerSecond, TimeSpan.Zero),
            new DateTimeOffset(DateTo * TimeSpan.TicksPerSecond, TimeSpan.Zero)
        );

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DateFrom;
        yield return DateTo;
    }
}
