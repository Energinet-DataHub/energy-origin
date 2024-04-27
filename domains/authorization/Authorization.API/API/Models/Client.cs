using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Client
{
    private Client(
        Guid id,
        IdpClientId idpClientId,
        Name name,
        Role role
    )
    {
        Id = id;
        IdpClientId = idpClientId;
        Name = name;
        Role = role;
    }

    public Guid Id { get; private set; }
    public IdpClientId IdpClientId { get; private set; }
    public Name Name { get; private set; }
    public Role Role { get; private set; }

    public ICollection<Consent> Consents { get; init; } = new List<Consent>();

    public static Client Create(
        IdpClientId idpClientId,
        Name name,
        Role role
    )
    {
        return new Client(
            Guid.NewGuid(),
            idpClientId,
            name,
            role
        );
    }
}
