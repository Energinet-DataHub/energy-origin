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

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = null!;
    public Guid ClientId { get; private set; }
    public Client Client { get; private set; } = null!;
    public DateTimeOffset ConsentDate { get; private set; }

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
