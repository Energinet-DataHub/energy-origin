using EnergyOrigin.ActivityLog.API;

namespace EnergyOrigin.ActivityLog.DataContext;

public class ActivityLogEntry
{
    // General
    public Guid Id { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    // User/Machine initiating the request
    public Guid ActorId { get; private set; } // Eks. Granular's id or Charlotte's id
    public ActivityLogResponse.ActorTypeEnum ActorType { get; private set; }
    public string ActorName { get; private set; } = ""; // Company name / person name

    // Owner
    public string OrganizationTin { get; private set; } = ""; // CVR
    public string OrganizationName { get; private set; } = ""; // Eks. "Mogens Mølleejer A/S"

    // Action
    public ActivityLogResponse.EntityTypeEnum EntityType { get; private set; }
    public ActivityLogResponse.ActionTypeEnum ActionType { get; private set; }
    public Guid EntityId { get; private set; }
}
