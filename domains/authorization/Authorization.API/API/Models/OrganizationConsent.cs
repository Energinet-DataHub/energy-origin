using System;

namespace API.Models;

public class OrganizationConsent : IEntity<Guid>
{
    public Guid Id { get; init; }
    public Guid ConsentGiverOrganizationId { get; init; }
    public Guid ConsentReceiverOrganizationId { get; init; }
    public DateTimeOffset ConsentDate { get; init; }

    public Organization ConsentGiverOrganization { get; init; } = null!;
    public Organization ConsentReceiverOrganization { get; init; } = null!;

    public static OrganizationConsent Create(Guid organizationGiverId, Guid organizationReceiverId, DateTimeOffset consentDate)
    {

        if (organizationGiverId == Guid.Empty)
        {
            throw new ArgumentException("organizationGiverId cannot be empty", nameof(organizationGiverId));
        }
        if (organizationReceiverId == Guid.Empty)
        {
            throw new ArgumentException("organizationReceiverId cannot be empty", nameof(organizationReceiverId));
        }

        var consent = new OrganizationConsent
        {
            Id = Guid.NewGuid(),
            ConsentDate = consentDate,
            ConsentReceiverOrganizationId = organizationReceiverId,
            ConsentGiverOrganizationId = organizationGiverId,
        };

        return consent;
    }
}
