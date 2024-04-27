using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class User(
    Guid id,
    IdpId idpId,
    IdpUserId idpUserId,
    Name name
    )
{
    public Guid Id { get; private set; } = id;
    public IdpId IdpId { get; private set; } = idpId;
    public IdpUserId IdpUserId { get; private set; } = idpUserId;
    public Name Name { get; private set; } = name;

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
}
