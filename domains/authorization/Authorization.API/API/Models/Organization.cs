using System;
using System.Collections.Generic;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;

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
        TermsVersion = null;
        TermsAcceptanceDate = null;
    }

    private Organization()
    {
    }

    public Guid Id { get; private set; }
    public Tin? Tin { get; private set; } = null!;
    public OrganizationName Name { get; private set; } = null!;
    public bool TermsAccepted { get; private set; }
    public int? TermsVersion { get; private set; }
    public DateTimeOffset? TermsAcceptanceDate { get; private set; }
    public bool ServiceProviderTermsAccepted { get; private set; }
    public int? ServiceProviderTermsVersion { get; private set; }
    public DateTimeOffset? ServiceProviderTermsAcceptanceDate { get; private set; }
    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
    public ICollection<OrganizationConsent> OrganizationGivenConsents { get; init; } = new List<OrganizationConsent>();
    public ICollection<OrganizationConsent> OrganizationReceivedConsents { get; init; } = new List<OrganizationConsent>();
    public ICollection<Client> Clients { get; init; } = new List<Client>();

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

    public void AcceptServiceProviderTerms(ServiceProviderTerms serviceProviderTerms)
    {
        ServiceProviderTermsAccepted = true;
        ServiceProviderTermsVersion = serviceProviderTerms.Version;
        ServiceProviderTermsAcceptanceDate = DateTimeOffset.UtcNow;
    }

    public void InvalidateServiceProviderTerms() => ServiceProviderTermsAccepted = false;

    public void AcceptTerms(Terms terms)
    {
        TermsAccepted = true;
        TermsVersion = terms.Version;
        TermsAcceptanceDate = DateTimeOffset.UtcNow;
    }

    public void InvalidateTerms() => TermsAccepted = false;

    public void RevokeTerms()
    {
        TermsAccepted = false;
        TermsVersion = null;
        TermsAcceptanceDate = null;
    }
}
