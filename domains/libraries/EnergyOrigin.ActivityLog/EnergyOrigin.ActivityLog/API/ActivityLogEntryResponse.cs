namespace EnergyOrigin.ActivityLog.API;

public class ActivityLogEntryResponse
{
    public enum ActorTypeEnum { User, System }
    public enum EntityTypeEnum { TransferAgreement, TransferAgreementProposal, MeteringPoint }
    public enum ActionTypeEnum { Created, Accepted, Declined, Activated, Deactivated, EndDateChanged, Expired }

    // General
    public Guid Id { get; init; }
    public long Timestamp { get; init; }

    // User/Machine initiating the request
    public Guid ActorId { get; init; } // Eks. Granular's id or Charlotte's id
    public ActorTypeEnum ActorType { get; init; }
    public string ActorName { get; init; } = ""; // Company name / person name

    // Owner
    public string OrganizationTin { get; init; } = ""; // CVR
    public string OrganizationName { get; init; } = ""; // Eks. "Mogens Mølleejer A/S"

    // Other organization
    public string OtherOrganizationTin { get; init; } = ""; // Other Organizations CVR
    public string OtherOrganizationName { get; init; } = ""; // Eks. "Producent A/S"

    // Action
    public EntityTypeEnum EntityType { get; init; }
    public ActionTypeEnum ActionType { get; init; }
    public string EntityId { get; init; } = "";
}
