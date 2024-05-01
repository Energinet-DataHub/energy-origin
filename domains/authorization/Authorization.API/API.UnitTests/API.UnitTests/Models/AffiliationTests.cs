using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class AffiliationTests
{
    [Fact]
    public void Affiliation_WithValidData_CreatesSuccessfully()
    {
        var organizationIdpId = IdpId.Create(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var idpIdForUser = IdpId.Create(Guid.NewGuid());
        var userName = Name.Create("Test User");
        var user = User.Create(idpIdForUser, idpUserId, userName);

        var affiliation = Affiliation.Create(user, organization);

        affiliation.Should().NotBeNull();
        affiliation.Id.Should().NotBeEmpty();
        affiliation.User.Should().Be(user);
        affiliation.Organization.Should().Be(organization);
        affiliation.UserId.Should().Be(user.Id);
        affiliation.OrganizationId.Should().Be(organization.Id);
    }

    [Fact]
    public void Affiliation_Create_AddsAffiliationToUserAndOrganization()
    {
        var organizationIdpId = IdpId.Create(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        var idpIdForUser = IdpId.Create(Guid.NewGuid());
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var userName = Name.Create("Test User");
        var user = User.Create(idpIdForUser, idpUserId, userName);

        var affiliation = Affiliation.Create(user, organization);

        user.Affiliations.Should().Contain(affiliation);
        organization.Affiliations.Should().Contain(affiliation);
    }

    [Fact]
    public void Affiliation_Create_ThrowsArgumentNullException_WhenUserIsNull()
    {
        var organizationIdpId = IdpId.Create(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        Action act = () => Affiliation.Create(null!, organization);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Affiliation_Create_ThrowsArgumentNullException_WhenOrganizationIsNull()
    {
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var idpIdForUser = IdpId.Create(Guid.NewGuid());
        var userName = Name.Create("Test User");
        var user = User.Create(idpIdForUser, idpUserId, userName);

        Action act = () => Affiliation.Create(user, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
