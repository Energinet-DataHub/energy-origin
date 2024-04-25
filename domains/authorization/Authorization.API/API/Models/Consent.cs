using System;

namespace API.Models;

public class Consent(Organization organization, Client client)
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Organization Organization { get; init; } = organization ?? throw new ArgumentNullException(nameof(organization));
    public Guid ClientId { get; init; }
    public Client Client { get; init; } = client ?? throw new ArgumentNullException(nameof(client));
    public DateTime ConsentDate { get; init; }
}
