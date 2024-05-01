using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class User : IEntity<Guid>
{
    private User(
        Guid id,
        IdpId idpId,
        IdpUserId idpUserId,
        Name name
    )
    {
        Id = id;
        IdpId = idpId;
        IdpUserId = idpUserId;
        Name = name;
    }

    private User()
    {
    }

    public Guid Id { get; private set; }
    public IdpId IdpId { get; private set; } = null!;
    public IdpUserId IdpUserId { get; private set; } = null!;
    public Name Name { get; private set; } = null!;

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();

    public static User Create(
        IdpId idpId,
        IdpUserId idpUserId,
        Name name
    )
    {
        return new User(
            Guid.NewGuid(),
            idpId,
            idpUserId,
            name
        );
    }
}
