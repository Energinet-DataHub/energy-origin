using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Organization
{
    private Organization(
        Guid id,
        IdpId idpId,
        IdpOrganizationId idpOrganizationId,
        Tin tin,
        OrganizationName organizationName
    )
    {
        Id = id;
        IdpId = idpId;
        IdpOrganizationId = idpOrganizationId;
        Tin = tin;
        OrganizationName = organizationName;
    }

    public Guid Id { get; private set; }
    public IdpId IdpId { get; private set; }
    public IdpOrganizationId IdpOrganizationId { get; private set; }
    public Tin Tin { get; private set; }
    public OrganizationName OrganizationName { get; private set; }

    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
    public ICollection<Consent> Consents { get; init; } = new List<Consent>();

    public static Organization Create(
        IdpId idpId,
        IdpOrganizationId idpOrganizationId,
        Tin tin,
        OrganizationName organizationName
    )
    {
        return new Organization(
            Guid.NewGuid(),
            idpId,
            idpOrganizationId,
            tin,
            organizationName
        );
    }
}
