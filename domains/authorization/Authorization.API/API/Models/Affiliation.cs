using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class Affiliation
{
    private Affiliation(
        Guid id,
        Guid userId,
        Guid organizationId,
        User user,
        Organization organization
    )
    {
        Id = id;
        User = user;
        Organization = organization;
        UserId = userId;
        OrganizationId = organizationId;
    }

    private Affiliation()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = null!;
    public User User { get; private set; } = null!;

    public static Affiliation Create(User user, Organization organization)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(organization);

        var affiliation = new Affiliation(Guid.NewGuid(), user.Id, organization.Id, user, organization);

        user.Affiliations.Add(affiliation);
        organization.Affiliations.Add(affiliation);

        return affiliation;
    }
}
