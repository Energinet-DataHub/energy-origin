using System;

namespace API.Models;

public class Affiliation
{
    private Affiliation(Guid id, Guid userId, Guid organizationId)
    {
        Id = id;
        UserId = userId;
        OrganizationId = organizationId;
    }

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;

    public static Affiliation Create(User user, Organization organization)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(organization);

        return new Affiliation(Guid.NewGuid(), user.Id, organization.Id)
        {
            User = user,
            Organization = organization
        };
    }
}
