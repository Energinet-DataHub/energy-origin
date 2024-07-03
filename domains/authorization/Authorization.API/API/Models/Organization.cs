using System;
using System.Collections.Generic;
using API.ValueObjects;

namespace API.Models;

public class Organization : IEntity<Guid>
{
    private Organization(
        Guid id,
        Tin tin,
        OrganizationName organizationName
    )
    {
        Id = id;
        Tin = tin;
        Name = organizationName;
        TermsAccepted = false;
        TermsVersion = string.Empty;
        TermsAcceptanceDate = null;
    }

    private Organization()
    {
    }

    public Guid Id { get; private set; }
    public Tin Tin { get; private set; } = null!;
    public OrganizationName Name { get; private set; } = null!;
    public bool TermsAccepted { get; private set; }
    public string? TermsVersion { get; private set; }
    public DateTimeOffset? TermsAcceptanceDate { get; private set; }
    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
    public ICollection<Consent> Consents { get; init; } = new List<Consent>();

    public static Organization Create(
        Tin tin,
        OrganizationName organizationName
    )
    {
        return new Organization(
            Guid.NewGuid(),
            tin,
            organizationName
        );
    }

    public void AcceptTerms(Terms terms)
    {
        TermsAccepted = true;
        TermsVersion = terms.Version;
        TermsAcceptanceDate = DateTimeOffset.UtcNow;
    }

    public void InvalidateTerms() => TermsAccepted = false;
}
