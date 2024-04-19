using System;
using System.Collections.Generic;

namespace API.Models;

public class User
{
    public Guid Id { get; set; }
    public string IdpId { get; set; } = string.Empty;
    public string IdpUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Affiliation> Affiliations { get; set; } = new List<Affiliation>();
}
