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

    public IReadOnlyCollection<Affiliation> Affiliations { get; init; } = Array.Empty<Affiliation>();
    public IReadOnlyCollection<Consent> Consents { get; init; } = Array.Empty<Consent>();
}
