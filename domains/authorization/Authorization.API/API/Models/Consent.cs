using System;

namespace API.Models;

public class Consent
{
    private Consent(
        Guid id,
        Guid organizationId,
        Guid clientId,
        Organization organization,
        Client client,
        DateTime consentDate
    )
    {
        Id = id;
        Organization = organization;
        Client = client;
        OrganizationId = organizationId;
        ClientId = clientId;
        ConsentDate = consentDate;
    }

    private Consent()
    {
    }

    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public DateTime ConsentDate { get; set; }

    public static Consent Create(Organization organization, Client client, DateTime consentDate)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(client);

        var consent = new Consent(
            Guid.NewGuid(),
            organization.Id,
            client.Id,
            organization,
            client,
            consentDate
        );

        organization.Consents.Add(consent);
        client.Consents.Add(consent);

        return consent;
    }
}
