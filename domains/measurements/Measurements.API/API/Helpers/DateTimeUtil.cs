namespace API.Helpers;

public static class DateTimeExtensionMethod
{
    public static DateTime ToDateTime(this long unixDateTimeSeconds) =>
        DateTimeOffset.FromUnixTimeSeconds(unixDateTimeSeconds).UtcDateTime;

    public static long ToUnixTime(this DateTime dateTime) =>
        ((DateTimeOffset)DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)).ToUnixTimeSeconds();
}
