using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Client : IEntity<Guid>
{
    private Client(Guid id, IdpClientId idpClientId, Role role, string redirectUrl) {
        Id = id;
        IdpClientId = idpClientId;
        Role = role;
        RedirectUrl = redirectUrl;
    }

    private Client()
    {
    }

    public Guid Id { get; private set; }
    public IdpClientId IdpClientId { get; private set; } = null!;
    public ClientName Name { get; private set; } = null!;
    public Role Role { get; private set; }

    public string RedirectUrl { get; set; } = null!;

    public ICollection<Consent> Consents { get; init; } = new List<Consent>();

    public static Client Create(IdpClientId idpClientId, Role role, string redirectUrl)
    {
        return new Client(Guid.NewGuid(), idpClientId, role, redirectUrl);
    }
}
