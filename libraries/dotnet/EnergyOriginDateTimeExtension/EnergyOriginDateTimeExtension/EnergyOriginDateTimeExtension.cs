namespace EnergyOriginDateTimeExtension
{
    public static class EnergyOriginDateTimeExtension
    {
        public static DateTime ToDateTime(this long date) => DateTimeOffset.FromUnixTimeSeconds(date).UtcDateTime;
        public static long ToUnixTime(this DateTime date) => ((DateTimeOffset)DateTime.SpecifyKind(date, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }
}
