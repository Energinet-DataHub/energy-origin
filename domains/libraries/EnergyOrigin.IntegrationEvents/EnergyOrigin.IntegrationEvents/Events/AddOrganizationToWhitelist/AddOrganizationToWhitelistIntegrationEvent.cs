namespace EnergyOrigin.IntegrationEvents.Events.AddOrganizationToWhitelist;

public record AddOrganizationToWhitelistIntegrationEvent : IntegrationEvent
{
    public string Tin { get; }

    public AddOrganizationToWhitelistIntegrationEvent(Guid id, string traceId, DateTimeOffset created, string tin)
        : base(id, traceId, created)
    {
        Tin = tin;
    }
}
