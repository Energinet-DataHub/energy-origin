
namespace API.Shared.Helpers;

public static class TimeSpanHelper
{
    public static int GetMinutesToNextHalfHour(int currentMinute)
    {
        var minutesToNextHalfHour = 30 - currentMinute;

        if (minutesToNextHalfHour < 0) minutesToNextHalfHour = 60 + minutesToNextHalfHour;

        return minutesToNextHalfHour;
    }
}
