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

    public Guid Id { get; }
    public IdpClientId IdpClientId { get; }
    public Name Name { get; }
    public Role Role { get; }

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
