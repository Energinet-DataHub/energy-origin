using API.Models;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class UserTests
{
    [Fact]
    public void User_WithValidData_CreatesSuccessfully()
    {
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var name = UserName.Create("Test User");

        var user = User.Create(idpUserId, name);

        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.IdpUserId.Should().Be(idpUserId);
        user.Name.Should().Be(name);
    }

    [Fact]
    public void User_CanExist_WithoutAffiliations()
    {
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var name = UserName.Create("Test User");

        var user = User.Create(idpUserId, name);

        user.Affiliations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_CanHave_Affiliations()
    {
        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var name = UserName.Create("Test User");

        var organizationTin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.Create(organizationTin, organizationName);

        var user = User.Create(idpUserId, name);
        var affiliation = Affiliation.Create(user, organization);

        user.Affiliations.Should().Contain(affiliation);
    }
}
