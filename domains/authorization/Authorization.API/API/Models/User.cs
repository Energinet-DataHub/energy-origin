using System;
using System.Collections.Generic;

namespace API.Models;

public class User
{
    public Guid Id { get; init; }
    public string IdpId { get; init; } = string.Empty;
    public string IdpUserId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
}
