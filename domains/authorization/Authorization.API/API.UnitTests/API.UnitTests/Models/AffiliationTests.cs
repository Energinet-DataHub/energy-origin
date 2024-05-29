using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class AffiliationTests
{
    [Fact]
    public void Affiliation_WithValidData_CreatesSuccessfully()
    {
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationTin, organizationName);

        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var userName = UserName.Create("Test User");
        var user = User.Create(idpUserId, userName);

        var affiliation = Affiliation.Create(user, organization);

        affiliation.Should().NotBeNull();
        affiliation.User.Should().Be(user);
        affiliation.Organization.Should().Be(organization);
        affiliation.UserId.Should().Be(user.Id);
        affiliation.OrganizationId.Should().Be(organization.Id);
    }

    [Fact]
    public void Affiliation_Create_AddsAffiliationToUserAndOrganization()
    {
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationTin, organizationName);

        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var userName = UserName.Create("Test User");
        var user = User.Create(idpUserId, userName);

        var affiliation = Affiliation.Create(user, organization);

        user.Affiliations.Should().Contain(affiliation);
        organization.Affiliations.Should().Contain(affiliation);
    }

    [Fact]
    public void Affiliation_Create_ThrowsArgumentNullException_WhenUserIsNull()
    {
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationTin, organizationName);

        Action act = () => Affiliation.Create(null!, organization);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Affiliation_Create_ThrowsArgumentNullException_WhenOrganizationIsNull()
    {
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var userName = UserName.Create("Test User");
        var user = User.Create(idpUserId, userName);

        Action act = () => Affiliation.Create(user, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
