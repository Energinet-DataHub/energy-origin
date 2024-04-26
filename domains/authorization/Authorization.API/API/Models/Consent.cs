using System;

namespace API.Models;

public class Consent
{
    private Consent(
        Guid id,
        Guid organizationId,
        Guid clientId,
        DateTime consentDate)
    {
        Id = id;
        OrganizationId = organizationId;
        ClientId = clientId;
        ConsentDate = consentDate;
    }

    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Organization Organization { get; init; } = null!;
    public Guid ClientId { get; init; }
    public Client Client { get; init; } = null!;
    public DateTime ConsentDate { get; init; }

    public static Consent Create(Organization organization, Client client, DateTime consentDate)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(client);

        return new Consent(Guid.NewGuid(), organization.Id, client.Id, consentDate)
        {
            Organization = organization,
            Client = client
        };
    }
}
