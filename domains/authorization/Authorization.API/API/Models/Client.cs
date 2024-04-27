using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Client(
    Guid id,
    IdpClientId idpClientId,
    Name name,
    Role role)
{
    public Guid Id { get; private set; } = id;
    public IdpClientId IdpClientId { get; private set; } = idpClientId;
    public Name Name { get; private set; } = name;
    public Role Role { get; private set; } = role;

    public ICollection<Consent> Consents { get; init; } = new List<Consent>();
}
