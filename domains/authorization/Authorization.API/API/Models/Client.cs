using System;
using API.ValueObjects;
using OrganizationId = EnergyOrigin.Domain.ValueObjects.OrganizationId;

namespace API.Models;

public class Client : IEntity<Guid>
{
    private Client(Guid id, IdpClientId idpClientId, ClientName name, ClientType clientType, string redirectUrl, bool isTrial)
    {
        Id = id;
        IdpClientId = idpClientId;
        Name = name;
        ClientType = clientType;
        RedirectUrl = redirectUrl;
        IsTrial = isTrial;
    }

    private Client()
    {
    }

    public Guid Id { get; private set; }
    public IdpClientId IdpClientId { get; private set; } = null!;
    public ClientName Name { get; private set; } = null!;
    public ClientType ClientType { get; private set; }
    public string RedirectUrl { get; set; } = null!;
    public Guid? OrganizationId { get; set; }
    public bool IsTrial { get; set; }

    public Organization? Organization { get; set; }

    public static Client Create(IdpClientId idpClientId, ClientName name, ClientType clientType, string redirectUrl, bool isTrial)
    {
        return new Client(Guid.NewGuid(), idpClientId, name, clientType, redirectUrl, isTrial);
    }

    public void SetOrganization(OrganizationId organizationId)
    {
        OrganizationId = organizationId.Value;
    }
}
