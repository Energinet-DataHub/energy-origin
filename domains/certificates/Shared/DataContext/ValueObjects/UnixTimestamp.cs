using System;
using System.Collections.Generic;

namespace DataContext.ValueObjects;

public class UnixTimestamp : ValueObject, IComparable<UnixTimestamp>
{
    public const long SecondsPerDay = 86400;
    public const long SecondsPerHour = 3600;
    public const long SecondsPerMinute = 60;
    public long Seconds { get; private set; }

    private UnixTimestamp()
    {
        Seconds = 0;
    }

    private UnixTimestamp(long seconds)
    {
        Seconds = seconds;
    }

    public static UnixTimestamp Now(TimeProvider timeProvider)
    {
        return new UnixTimestamp(timeProvider.GetUtcNow().ToUnixTimeSeconds());
    }

    public static UnixTimestamp Now()
    {
        return new UnixTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public UnixTimestamp RoundToLatestHour()
    {
        return new UnixTimestamp(Seconds - Seconds % SecondsPerHour);
    }

    public UnixTimestamp RoundToNextHour()
    {
        return Seconds % SecondsPerHour > 0 ? new UnixTimestamp(Seconds + (3600 - Seconds % SecondsPerHour)) : this;
    }

    public TimeSpan TimeUntilNextHour()
    {
        var lastHour = RoundToLatestHour();
        var secondsIntoThisHour = Seconds - lastHour.Seconds;
        return TimeSpan.FromSeconds(SecondsPerHour - secondsIntoThisHour);
    }

    public UnixTimestamp RoundToLatestMidnight()
    {
        return new UnixTimestamp(Seconds - Seconds % SecondsPerDay);
    }

    public static UnixTimestamp Create(long seconds)
    {
        return new UnixTimestamp(seconds);
    }

    public static UnixTimestamp Create(DateTimeOffset timestamp)
    {
        return new UnixTimestamp(timestamp.ToUnixTimeSeconds());
    }

    public static UnixTimestamp Max(UnixTimestamp a, UnixTimestamp b)
    {
        return a.Seconds > b.Seconds ? a : b;
    }

    public static UnixTimestamp Empty()
    {
        return new UnixTimestamp();
    }

    public DateTimeOffset ToDateTimeOffset()
    {
        return DateTimeOffset.FromUnixTimeSeconds(Seconds);
    }

    public UnixTimestamp Add(TimeSpan timespan)
    {
        return new UnixTimestamp(this.Seconds + (long)timespan.TotalSeconds);
    }

    public int CompareTo(UnixTimestamp? other)
    {
        if (other == null) return 1; // By convention, any object compares greater than null
        return Seconds.CompareTo(other.Seconds);
    }

    public static bool operator >=(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.Seconds >= t2.Seconds;
    }

    public static bool operator <=(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.Seconds <= t2.Seconds;
    }

    public static bool operator >(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.Seconds > t2.Seconds;
    }

    public static bool operator <(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.Seconds < t2.Seconds;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        return new object[] { Seconds };
    }

    public override string ToString()
    {
        return DateTimeOffset.FromUnixTimeSeconds(Seconds).ToString();
    }
}
