using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Client : IEntity<Guid>
{
    private Client(Guid id, IdpClientId idpClientId, ClientName name, ClientType clientType, string redirectUrl)
    {
        Id = id;
        IdpClientId = idpClientId;
        Name = name;
        ClientType = clientType;
        RedirectUrl = redirectUrl;
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

    public Organization? Organization { get; set; }

    public static Client Create(IdpClientId idpClientId, ClientName name, ClientType clientType, string redirectUrl)
    {
        return new Client(Guid.NewGuid(), idpClientId, name, clientType, redirectUrl);
    }
}
