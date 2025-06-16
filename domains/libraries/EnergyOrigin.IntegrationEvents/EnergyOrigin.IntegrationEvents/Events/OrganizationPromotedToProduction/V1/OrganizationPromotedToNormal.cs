using System.Diagnostics;

namespace EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;

public record OrganizationPromotedToNormal : IntegrationEvent
{
    public Guid OrganizationId { get; }

    public OrganizationPromotedToNormal(Guid id, string traceId, DateTimeOffset created, Guid organizationId)
        : base(id, traceId, created)
    {
        OrganizationId = organizationId;
    }

    public static OrganizationPromotedToNormal Create(Guid organizationId)
    {
        return new OrganizationPromotedToNormal(Guid.NewGuid(), Activity.Current?.Id ?? Guid.NewGuid().ToString(), DateTimeOffset.UtcNow,
            organizationId);
    }
}
