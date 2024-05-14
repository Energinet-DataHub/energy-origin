using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class OrganizationTests
{
    [Fact]
    public void Organization_WithValidData_CreatesSuccessfully()
    {
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.Should().NotBeNull();
        organization.Id.Should().NotBeEmpty();
        organization.Tin.Should().Be(tin);
    }

    [Fact]
    public void Organization_CanExist_WithoutAffiliations()
    {
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.Affiliations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Organization_CanExist_WithoutConsents()
    {
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.Consents.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Organization_CanHave_Affiliations()
    {
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var userName = UserName.Create("Test User");
        var user = User.Create(idpUserId, userName);

        var affiliation = Affiliation.Create(user, organization);

        organization.Affiliations.Should().Contain(affiliation);
    }

    [Fact]
    public void Organization_CanHave_Consents()
    {
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        var idpClientId = new IdpClientId(Guid.NewGuid());
        var role = ClientType.External;
        var client = Client.Create(idpClientId, new ClientName("Client"), role, "https://redirect.url");

        var consentDate = DateTimeOffset.UtcNow;
        var consent = Consent.Create(organization, client, consentDate);

        organization.Consents.Should().Contain(consent);
    }
}
