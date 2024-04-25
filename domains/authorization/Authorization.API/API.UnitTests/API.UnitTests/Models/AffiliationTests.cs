using API.Models;
using Xunit;

namespace API.UnitTests.Models;

public class AffiliationTests
{
    [Fact]
    public void Affiliation_WithValidData_CreatesSuccessfully()
    {
        var user = new User();
        var organization = new Organization();

        var affiliation = Affiliation.Create(user, organization);

        Assert.NotNull(affiliation);
        Assert.Equal(user.Id, affiliation.UserId);
        Assert.Equal(organization.Id, affiliation.OrganizationId);
        Assert.Same(user, affiliation.User);
        Assert.Same(organization, affiliation.Organization);
    }

    [Fact]
    public void Affiliation_CreateWithNoUser_ThrowsException()
    {
        var organization = new Organization();

        Assert.Throws<ArgumentNullException>(() => Affiliation.Create(null!, organization));
    }

    [Fact]
    public void Affiliation_CreateWithNoOrganization_ThrowsException()
    {
        var user = new User();

        Assert.Throws<ArgumentNullException>(() => Affiliation.Create(user, null!));
    }
}
