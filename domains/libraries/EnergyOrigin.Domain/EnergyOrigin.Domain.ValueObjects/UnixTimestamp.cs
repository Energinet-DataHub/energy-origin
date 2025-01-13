namespace EnergyOrigin.Domain.ValueObjects;

public class UnixTimestamp : ValueObject
{
    public const long SecondsPerDay = 86400;
    public const long SecondsPerHour = 3600;
    public const long SecondsPerMinute = 60;

    public long EpochSeconds { get; private set; }

    private UnixTimestamp()
    {
        EpochSeconds = 0;
    }

    private UnixTimestamp(long epochSeconds)
    {
        EpochSeconds = epochSeconds;
    }

    public static UnixTimestamp Now()
    {
        return new UnixTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public UnixTimestamp RoundToLatestHour()
    {
        return new UnixTimestamp(EpochSeconds - EpochSeconds % SecondsPerHour);
    }

    public UnixTimestamp RoundToNextHour()
    {
        return EpochSeconds % SecondsPerHour > 0 ? new UnixTimestamp(EpochSeconds + (3600 - EpochSeconds % SecondsPerHour)) : this;
    }

    public TimeSpan TimeUntilNextHour()
    {
        var lastHour = RoundToLatestHour();
        var secondsIntoThisHour = EpochSeconds - lastHour.EpochSeconds;
        return TimeSpan.FromSeconds(SecondsPerHour - secondsIntoThisHour);
    }

    public UnixTimestamp RoundToLatestMidnight()
    {
        return new UnixTimestamp(EpochSeconds - EpochSeconds % SecondsPerDay);
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
        return a.EpochSeconds > b.EpochSeconds ? a : b;
    }

    public static UnixTimestamp Min(UnixTimestamp a, UnixTimestamp b)
    {
        return a.EpochSeconds < b.EpochSeconds ? a : b;
    }

    public static UnixTimestamp Empty()
    {
        return new UnixTimestamp();
    }

    public DateTimeOffset ToDateTimeOffset()
    {
        return DateTimeOffset.FromUnixTimeSeconds(EpochSeconds);
    }

    public UnixTimestamp Add(TimeSpan timespan)
    {
        return new UnixTimestamp(this.EpochSeconds + (long)timespan.TotalSeconds);
    }

    public static bool operator >=(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.EpochSeconds >= t2.EpochSeconds;
    }

    public static bool operator <=(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.EpochSeconds <= t2.EpochSeconds;
    }

    public static bool operator >(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.EpochSeconds > t2.EpochSeconds;
    }

    public static bool operator <(UnixTimestamp t1, UnixTimestamp t2)
    {
        return t1.EpochSeconds < t2.EpochSeconds;
    }

    public override string ToString()
    {
        return DateTimeOffset.FromUnixTimeSeconds(EpochSeconds).ToString();
    }

    public UnixTimestamp AddMinutes(int minutes)
    {
        return this.Add(TimeSpan.FromMinutes(minutes));
    }

    public UnixTimestamp AddHours(int hours)
    {
        return this.Add(TimeSpan.FromHours(hours));
    }

    public UnixTimestamp AddDays(int days)
    {
        return this.Add(TimeSpan.FromDays(days));
    }

    public UnixTimestamp AddYears(int years)
    {
        return new UnixTimestamp(DateTimeOffset.FromUnixTimeSeconds(EpochSeconds).AddYears(years).ToUnixTimeSeconds());
    }
}
