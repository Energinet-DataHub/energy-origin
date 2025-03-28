using System.Diagnostics;

namespace EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist;

public record OrganizationRemovedFromWhitelist : IntegrationEvent
{
    public Guid OrganizationId { get; }
    public string Tin { get; }

    public OrganizationRemovedFromWhitelist(Guid id, string traceId, DateTimeOffset created, Guid organizationId, string tin)
        : base(id, traceId, created)
    {
        OrganizationId = organizationId;
        Tin = tin;
    }

    public static OrganizationRemovedFromWhitelist Create(Guid organizationId, string tin)
    {
        return new OrganizationRemovedFromWhitelist(Guid.NewGuid(), Activity.Current?.Id ?? Guid.NewGuid().ToString(), DateTimeOffset.UtcNow,
            organizationId, tin);
    }
}
