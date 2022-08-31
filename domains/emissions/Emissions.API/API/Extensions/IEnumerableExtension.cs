using API.Shared.Models;

namespace API.Extensions;

public static class IEnumerableExtension
{
    public static IEnumerable<IGrouping<DateTimeOffset, T>> GroupByAggregation<T>(this IEnumerable<T> list, Func<T, DateTimeOffset> selector, Aggregation aggregation) => list.GroupBy(x => selector(x).ToAggregation(aggregation));

    private static DateTimeOffset ToAggregation(this DateTimeOffset time, Aggregation aggregation) => aggregation switch
    {
        Aggregation.Year => new DateTimeOffset(time.Year, 1, 1, 0, 0, 0, time.Offset),
        Aggregation.Month => new DateTimeOffset(time.Year, time.Month, 1, 0, 0, 0, time.Offset),
        Aggregation.Day => new DateTimeOffset(time.Year, time.Month, time.Day, 0, 0, 0, time.Offset),
        Aggregation.Hour => new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, 0, 0, time.Offset),
        Aggregation.QuarterHour => new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, time.Minute.RoundDownToMultiplier(15), 0, time.Offset),
        Aggregation.Actual => time,
        Aggregation.Total => new DateTimeOffset(0, time.Offset),
        _ => throw new ArgumentOutOfRangeException($"Invalid value {aggregation}"),
    };

    private static int RoundDownToMultiplier(this int i, int multiplier) => i / multiplier * multiplier;
}
