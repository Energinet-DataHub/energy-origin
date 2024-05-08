using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class User : IEntity<Guid>
{
    private User(
        Guid id,
        IdpUserId idpUserId,
        Username username
    )
    {
        Id = id;
        IdpUserId = idpUserId;
        Username = username;
    }

    private User()
    {
    }

    public Guid Id { get; private set; }
    public IdpUserId IdpUserId { get; private set; } = null!;
    public Username Username { get; private set; } = null!;

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();

    public static User Create(
        IdpId idpId,
        IdpUserId idpUserId,
        Username username
    )
    {
        return new User(
            Guid.NewGuid(),
            idpUserId,
            username
        );
    }
}
