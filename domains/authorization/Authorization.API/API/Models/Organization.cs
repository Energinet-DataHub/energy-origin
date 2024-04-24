using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Organization
{
    public Guid Id { get; set; }
    public string IdpId { get; set; } = string.Empty;
    public string IdpOrganizationId { get; set; } = string.Empty;
    public Tin Tin { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Affiliation> Affiliations { get; set; } = new List<Affiliation>();
    public ICollection<Consent> Consents { get; set; } = new List<Consent>();
}
