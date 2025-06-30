using API.ValueObjects;

namespace API.Events;

public class OrganizationPromotedToNormalDomainEvent : IDomainEvent
{
    public OrganizationId OrganizationId { get; }

    private OrganizationPromotedToNormalDomainEvent(OrganizationId organizationId)
    {
        OrganizationId = organizationId;
    }

    public static OrganizationPromotedToNormalDomainEvent Create(OrganizationId organizationId)
    {
        return new OrganizationPromotedToNormalDomainEvent(organizationId);
    }
}
