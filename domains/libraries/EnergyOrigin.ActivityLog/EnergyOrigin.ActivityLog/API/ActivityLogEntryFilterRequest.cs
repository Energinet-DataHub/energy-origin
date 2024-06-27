namespace EnergyOrigin.ActivityLog.API;

public record ActivityLogEntryFilterRequest(
    long? Start,
    long? End,
    ActivityLogEntryResponse.EntityTypeEnum? EntityType
    );
