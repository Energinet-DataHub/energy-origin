namespace EnergyOrigin.ActivityLog.API;

public class ActivityLogEntryResponse
{
    public enum ActorTypeEnum { User, System }
    public enum EntityTypeEnum { TransferAgreement, MeteringPoint, TransferAgreementProposal }
    public enum ActionTypeEnum { Created, Accepted, Declined, Activated, Deactivated, ChangeEndDate }

    // General
    public Guid Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    // User/Machine initiating the request
    public Guid ActorId { get; init; } // Eks. Granular's id or Charlotte's id
    public ActorTypeEnum ActorType { get; init; }
    public string ActorName { get; init; } = ""; // Company name / person name

    // Owner
    public string OrganizationTin { get; init; } = ""; // CVR
    public string OrganizationName { get; init; } = ""; // Eks. "Mogens Mølleejer A/S"

    // Action
    public EntityTypeEnum EntityType { get; init; }
    public ActionTypeEnum ActionType { get; init; }
    public Guid EntityId { get; init; }
}
