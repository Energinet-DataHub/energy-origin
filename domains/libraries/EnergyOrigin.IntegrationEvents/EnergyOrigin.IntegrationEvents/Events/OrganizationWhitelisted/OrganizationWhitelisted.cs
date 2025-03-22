namespace EnergyOrigin.IntegrationEvents.Events.OrganizationWhitelisted;

public record OrganizationWhitelistedIntegrationEvent : IntegrationEvent
{
    public string Tin { get; }

    public OrganizationWhitelistedIntegrationEvent(Guid id, string traceId, DateTimeOffset created, string tin)
        : base(id, traceId, created)
    {
        Tin = tin;
    }
}
