namespace EnergyOrigin.ActivityLog.API;

public record ActivityLogEntryFilterRequest(DateTimeOffset? Start, DateTimeOffset? End, ActivityLogResponse.EntityTypeEnum? EntityType);
