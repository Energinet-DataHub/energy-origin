using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class User
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

    public Guid Id { get; set; }
    public IdpId IdpId { get; set; } = null!;
    public IdpUserId IdpUserId { get; set; } = null!;
    public Name Name { get; set; } = null!;

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
