using System.Diagnostics;

namespace EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;

public record OrganizationPromotedToNormal : IntegrationEvent
{
    public Guid OrganizationId { get; }
    public string Tin { get; }

    public OrganizationPromotedToNormal(Guid id, string traceId, DateTimeOffset created, Guid organizationId, string tin)
        : base(id, traceId, created)
    {
        OrganizationId = organizationId;
        Tin = tin;
    }

    public static OrganizationPromotedToNormal Create(Guid organizationId, string tin)
    {
        return new OrganizationPromotedToNormal(Guid.NewGuid(), Activity.Current?.Id ?? Guid.NewGuid().ToString(), DateTimeOffset.UtcNow,
            organizationId, tin);
    }
}
