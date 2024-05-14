using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class User : IEntity<Guid>
{
    private User(
        Guid id,
        IdpUserId idpUserId,
        UserName name
    )
    {
        Id = id;
        IdpUserId = idpUserId;
        Name = name;
    }

    private User()
    {
    }

    public Guid Id { get; private set; }
    public IdpUserId IdpUserId { get; private set; } = null!;
    public UserName Name { get; private set; } = null!;

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();

    public static User Create(
        IdpUserId idpUserId,
        UserName name
    )
    {
        return new User(
            Guid.NewGuid(),
            idpUserId,
            name
        );
    }
}
