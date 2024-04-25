using API.Models;

namespace API.UnitTests.Models;

public class AffiliationTests
{
    [Fact]
    public void Affiliation_WithValidData_CreatesSuccessfully()
    {
        var id = Guid.NewGuid();
        var user = new User();
        var organization = new Organization();

        var affiliation = new Affiliation(user, organization)
        {
            Id = id,
            UserId = user.Id,
            OrganizationId = organization.Id
        };

        Assert.Equal(id, affiliation.Id);
        Assert.Equal(user.Id, affiliation.UserId);
        Assert.Equal(organization.Id, affiliation.OrganizationId);
        Assert.Same(user, affiliation.User);
        Assert.Same(organization, affiliation.Organization);
    }

    [Fact]
    public void Affiliation_WithNoUser_ThrowsException()
    {
        var organization = new Organization();

        Assert.Throws<ArgumentNullException>(() => new Affiliation(null!, organization));
    }

    [Fact]
    public void Affiliation_WithNoOrganization_ThrowsException()
    {
        var user = new User();

        Assert.Throws<ArgumentNullException>(() => new Affiliation(user, null!));
    }
}
