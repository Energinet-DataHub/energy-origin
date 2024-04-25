using System;

namespace API.Models;

public class Affiliation(User user, Organization organization)
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public User User { get; init; } = user ?? throw new ArgumentNullException(nameof(user));
    public Guid OrganizationId { get; init; }
    public Organization Organization { get; init; } = organization ?? throw new ArgumentNullException(nameof(organization));
}
