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

    private Organization()
    {
    }

    public Guid Id { get; set; }
    public IdpId IdpId { get; set; } = null!;
    public IdpOrganizationId IdpOrganizationId { get; set; } = null!;
    public Tin Tin { get; set; } = null!;
    public OrganizationName OrganizationName { get; set; } = null!;

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
