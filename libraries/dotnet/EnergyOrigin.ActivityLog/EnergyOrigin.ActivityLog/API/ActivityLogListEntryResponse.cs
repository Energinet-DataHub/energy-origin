namespace EnergyOrigin.ActivityLog.API;

public class ActivityLogListEntryResponse
{
    public IEnumerable<ActivityLogEntryResponse> ActivityLogEntries { get; init; } = new List<ActivityLogEntryResponse>();
    public bool HasMore { get; init; }
}
