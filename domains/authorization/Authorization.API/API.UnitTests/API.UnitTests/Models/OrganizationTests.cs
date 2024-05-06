using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class OrganizationTests
{
    [Fact]
    public void Organization_WithValidData_CreatesSuccessfully()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        organization.Should().NotBeNull();
        organization.Id.Should().NotBeEmpty();
        organization.IdpId.Should().Be(idpId);
        organization.IdpOrganizationId.Should().Be(idpOrganizationId);
        organization.Tin.Should().Be(tin);
        organization.OrganizationName.Should().Be(organizationName);
    }

    [Fact]
    public void Organization_CanExist_WithoutAffiliations()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        organization.Affiliations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Organization_CanExist_WithoutConsents()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        organization.Consents.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Organization_CanHave_Affiliations()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        var idpIdForUser = IdpId.Create(Guid.NewGuid());
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var userName = Name.Create("Test User");
        var user = User.Create(idpIdForUser, idpUserId, userName);

        var affiliation = Affiliation.Create(user, organization);

        organization.Affiliations.Should().Contain(affiliation);
    }

    [Fact]
    public void Organization_CanHave_Consents()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        var idpClientId = new IdpClientId(Guid.NewGuid());
        var clientName = OrganizationName.Create("Test Client");
        var role = Role.External;
        var client = Client.Create(idpClientId, clientName, role);

        var consentDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var consent = Consent.Create(organization, client, consentDate);

        organization.Consents.Should().Contain(consent);
    }
}
