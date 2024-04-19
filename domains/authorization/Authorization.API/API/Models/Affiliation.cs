using System;

namespace API.Models;

public class Affiliation
{
    public Guid UserId { get; set; }
    public User User { get; set; } = new();
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = new();
}
