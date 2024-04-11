namespace Transfer.Domain;

public interface ISystemTime
{
    int GetMinutesToNextHalfHour(int currentMinute);
}

public class SystemTime : ISystemTime
{
    public int GetMinutesToNextHalfHour(int currentMinute)
    {
        if (currentMinute == 30) return 60;

        var minutesToNextHalfHour = 30 - currentMinute;

        if (minutesToNextHalfHour < 0) minutesToNextHalfHour = 60 + minutesToNextHalfHour;

        return minutesToNextHalfHour;
    }
}
##trigger workflow