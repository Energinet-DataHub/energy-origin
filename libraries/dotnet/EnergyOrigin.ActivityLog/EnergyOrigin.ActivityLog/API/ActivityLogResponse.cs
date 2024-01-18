namespace EnergyOrigin.ActivityLog.API;

public class ActivityLogResponse
{
    public enum ActorTypeEnum { User, System }
    public enum EntityTypeEnum { TransferAgreement, MeteringPoint }
    public enum ActionTypeEnum { Created, Accepted, Declined, Activated, Deactivated, ChangeEndDate }

    // General
    public Guid Id { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    // User/Machine initiating the request
    public Guid ActorId { get; private set; } // Eks. Granular's id or Charlotte's id
    public ActorTypeEnum ActorType { get; private set; }
    public string ActorName { get; private set; } = ""; // Company name / person name

    // Owner
    public string OrganizationTin { get; private set; } = ""; // CVR
    public string OrganizationName { get; private set; } = ""; // Eks. "Mogens Mølleejer A/S"

    // Action
    public EntityTypeEnum EntityType { get; private set; }
    public ActionTypeEnum ActionType { get; private set; }
    public Guid EntityId { get; private set; }
}

public class ResponseTest
{
    List<ActivityLogResponse> Data { get; set; } = new ();
    bool HasMore { get; set; }
}
