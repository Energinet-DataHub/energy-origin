using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Client : IEntity<Guid>
{
    private Client(Guid id, IdpClientId idpClientId, OrganizationName organizationName, Role role) {
        Id = id;
        IdpClientId = idpClientId;
        OrganizationName = organizationName;
        Role = role;
    }

    private Client()
    {
    }

    public Guid Id { get; private set; }
    public IdpClientId IdpClientId { get; private set; } = null!;
    public OrganizationName OrganizationName { get; private set; } = null!;
    public Role Role { get; private set; }

    public ICollection<Consent> Consents { get; init; } = new List<Consent>();

    public static Client Create(IdpClientId idpClientId, OrganizationName organizationName, Role role)
    {
        return new Client(Guid.NewGuid(), idpClientId, organizationName, role);
    }
}
