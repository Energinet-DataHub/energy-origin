namespace EnergyOrigin.ActivityLog.DataContext;

public class ActivityLogEntry
{
    private ActivityLogEntry()
    {
    }

    private ActivityLogEntry(Guid id, DateTimeOffset timestamp, Guid actorId, ActorTypeEnum actorType, string actorName, string organizationTin,
        string organizationName, EntityTypeEnum entityType, ActionTypeEnum actionType, Guid entityId)
    {
        Id = id;
        Timestamp = timestamp;
        ActorId = actorId;
        ActorType = actorType;
        ActorName = actorName;
        OrganizationTin = organizationTin;
        OrganizationName = organizationName;
        EntityType = entityType;
        ActionType = actionType;
        EntityId = entityId;
    }

    public static ActivityLogEntry Create(Guid actorId, ActorTypeEnum actorType, string actorName, string organizationTin, string organizationName,
        EntityTypeEnum entityType, ActionTypeEnum actionType, Guid entityId)
    {
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        return new ActivityLogEntry(id, timestamp, actorId, actorType, actorName, organizationTin, organizationName, entityType, actionType, entityId);
    }

    public enum ActorTypeEnum
    {
        User,
        System
    }

    public enum EntityTypeEnum
    {
        TransferAgreement,
        TransferAgreementProposal,
        MeteringPoint
    }

    public enum ActionTypeEnum
    {
        Created,
        Accepted,
        Declined,
        Activated,
        Deactivated,
        ChangeEndDate
    }

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
