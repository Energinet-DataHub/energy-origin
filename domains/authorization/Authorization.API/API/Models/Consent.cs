using System;

namespace API.Models;

public class Consent
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Organization Organization { get; init; } = new();
    public Guid ClientId { get; init; }
    public Client Client { get; init; } = new();
    public DateTime ConsentDate { get; init; }
}
