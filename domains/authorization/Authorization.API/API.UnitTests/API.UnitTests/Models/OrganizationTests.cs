using API.Models;
using API.ValueObjects;

namespace API.UnitTests.Models;

public class OrganizationTests
{
    [Fact]
    public void Organization_WithValidData_CreatesSuccessfully()
    {
        var id = Guid.NewGuid();
        var idpId = Guid.NewGuid();
        var idpOrganizationId = Guid.NewGuid();
        var tin = new Tin("00000000");
        var organizationName = new OrganizationName("Test Organization");

        var organization = new Organization
        {
            Id = id,
            IdpId = idpId,
            IdpOrganizationId = idpOrganizationId,
            Tin = tin,
            OrganizationName = organizationName
        };

        Assert.Equal(id, organization.Id);
        Assert.Equal(idpId, organization.IdpId);
        Assert.Equal(idpOrganizationId, organization.IdpOrganizationId);
        Assert.Equal(tin, organization.Tin);
        Assert.Equal(organizationName, organization.OrganizationName);
    }

    [Fact]
    public void Organization_WithAffiliations_InitializesCorrectly()
    {
        var affiliations = new List<Affiliation>
        {
            new() { Id = Guid.NewGuid() }
        };

        var organization = new Organization { Affiliations = affiliations };

        Assert.Equal(affiliations, organization.Affiliations);
    }

    [Fact]
    public void Organization_WithConsents_InitializesCorrectly()
    {
        var consents = new List<Consent>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };

        var organization = new Organization { Consents = consents };

        Assert.Equal(consents, organization.Consents);
    }

    [Fact]
    public void Organization_WithEmptyAffiliations_InitializesEmptyList()
    {
        var organization = new Organization();

        Assert.Empty(organization.Affiliations);
    }

    [Fact]
    public void Organization_WithEmptyConsents_InitializesEmptyList()
    {
        var organization = new Organization();

        Assert.Empty(organization.Consents);
    }
}
