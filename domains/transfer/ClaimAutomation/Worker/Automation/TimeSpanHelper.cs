
namespace ClaimAutomation.Worker.Automation;

public static class TimeSpanHelper
{
    public static int GetMinutesToNextHalfHour(int currentMinute)
    {
        if (currentMinute == 30) return 60;

        var minutesToNextHalfHour = 30 - currentMinute;

        if (minutesToNextHalfHour < 0) minutesToNextHalfHour = 60 + minutesToNextHalfHour;

        return minutesToNextHalfHour;
    }
}
