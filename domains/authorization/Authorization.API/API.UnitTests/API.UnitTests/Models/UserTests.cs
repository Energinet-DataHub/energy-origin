using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class UserTests
{
    [Fact]
    public void User_WithValidData_CreatesSuccessfully()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var name = Username.Create("Test User");

        var user = User.Create(idpId, idpUserId, name);

        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.IdpUserId.Should().Be(idpUserId);
        user.Username.Should().Be(name);
    }

    [Fact]
    public void User_CanExist_WithoutAffiliations()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var name = Username.Create("Test User");

        var user = User.Create(idpId, idpUserId, name);

        user.Affiliations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_CanHave_Affiliations()
    {
        var idpId = IdpId.Create(Guid.NewGuid());
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var name = Username.Create("Test User");

        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(organizationTin, organizationName);

        var user = User.Create(idpId, idpUserId, name);
        var affiliation = Affiliation.Create(user, organization);

        user.Affiliations.Should().Contain(affiliation);
    }
}
