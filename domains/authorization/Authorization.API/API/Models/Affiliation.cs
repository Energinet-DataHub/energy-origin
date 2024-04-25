using System;

namespace API.Models;

public class Affiliation
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public User User { get; init; } = new();
    public Guid OrganizationId { get; init; }
    public Organization Organization { get; init; } = new();
}
