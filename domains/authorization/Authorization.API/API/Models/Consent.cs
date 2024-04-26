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

    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Organization Organization { get; }
    public Guid ClientId { get; }
    public Client Client { get; }
    public DateTime ConsentDate { get; }

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
