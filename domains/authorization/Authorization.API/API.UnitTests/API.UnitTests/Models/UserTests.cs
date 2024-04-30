using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class UserTests
{
    [Fact]
    public void User_WithValidData_CreatesSuccessfully()
    {
        var idpId = new IdpId(Guid.NewGuid());
        var idpUserId = new IdpUserId(Guid.NewGuid());
        var name = new Name("Test User");

        var user = User.Create(idpId, idpUserId, name);

        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.IdpId.Should().Be(idpId);
        user.IdpUserId.Should().Be(idpUserId);
        user.Name.Should().Be(name);
    }

    [Fact]
    public void User_CanExist_WithoutAffiliations()
    {
        var idpId = new IdpId(Guid.NewGuid());
        var idpUserId = new IdpUserId(Guid.NewGuid());
        var name = new Name("Test User");

        var user = User.Create(idpId, idpUserId, name);

        user.Affiliations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_CanHave_Affiliations()
    {
        var idpId = new IdpId(Guid.NewGuid());
        var idpUserId = new IdpUserId(Guid.NewGuid());
        var name = new Name("Test User");

        var organizationIdpId = new IdpId(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        var user = User.Create(idpId, idpUserId, name);
        var affiliation = Affiliation.Create(user, organization);

        user.Affiliations.Should().Contain(affiliation);
    }
}
