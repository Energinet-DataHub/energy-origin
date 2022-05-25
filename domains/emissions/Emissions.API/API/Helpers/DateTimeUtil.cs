namespace API.Helpers;

public class DateTimeUtil
{
    public static DateTime ToUtcDateTime(long unixDateTimeSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixDateTimeSeconds).UtcDateTime;
    }

    public static long ToUnixTime(DateTime dateTime)
    {
        return ((DateTimeOffset)DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }
}