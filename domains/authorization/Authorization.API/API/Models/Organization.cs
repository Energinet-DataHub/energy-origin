using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Organization(
    Guid id,
    IdpId idpId,
    IdpOrganizationId idpOrganizationId,
    Tin tin,
    OrganizationName organizationName)
{
    public Guid Id { get; private set; } = id;
    public IdpId IdpId { get; private set; } = idpId;
    public IdpOrganizationId IdpOrganizationId { get; private set; } = idpOrganizationId;
    public Tin Tin { get; private set; } = tin;
    public OrganizationName OrganizationName { get; private set; } = organizationName;

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
    public ICollection<Consent> Consents { get; init; } = new List<Consent>();



}
