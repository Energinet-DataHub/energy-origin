using System;

namespace API.Models;

public class Affiliation
{
    private Affiliation(
        Guid userId,
        Guid organizationId,
        User user,
        Organization organization
    )
    {
        User = user;
        Organization = organization;
        UserId = userId;
        OrganizationId = organizationId;
    }

    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; }
    public User User { get; private set; }

    public static Affiliation Create(User user, Organization organization)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(organization);

        var affiliation = new Affiliation(user.Id, organization.Id, user, organization);

        user.Affiliations.Add(affiliation);
        organization.Affiliations.Add(affiliation);

        return affiliation;
    }
}
