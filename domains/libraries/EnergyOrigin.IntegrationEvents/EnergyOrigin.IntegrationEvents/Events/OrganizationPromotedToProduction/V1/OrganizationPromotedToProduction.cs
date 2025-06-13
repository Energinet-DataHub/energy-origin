using System.Diagnostics;

namespace EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;

public record OrganizationPromotedToProduction : IntegrationEvent
{
    public Guid OrganizationId { get; }
    public string Tin { get; }

    public OrganizationPromotedToProduction(Guid id, string traceId, DateTimeOffset created, Guid organizationId, string tin)
        : base(id, traceId, created)
    {
        OrganizationId = organizationId;
        Tin = tin;
    }

    public static OrganizationPromotedToProduction Create(Guid organizationId, string tin)
    {
        return new OrganizationPromotedToProduction(Guid.NewGuid(), Activity.Current?.Id ?? Guid.NewGuid().ToString(), DateTimeOffset.UtcNow,
            organizationId, tin);
    }
}
