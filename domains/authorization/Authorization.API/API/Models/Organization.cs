using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Organization
{
    public Guid Id { get; init; }
    public Guid IdpId { get; init; }
    public Guid IdpOrganizationId { get; init; }
    public Tin Tin { get; init; }
    public OrganizationName OrganizationName { get; init; }

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
    public ICollection<Consent> Consents { get; init; } = new List<Consent>();
}
