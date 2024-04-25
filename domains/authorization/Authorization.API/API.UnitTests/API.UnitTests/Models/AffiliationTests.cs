using API.Models;

namespace API.UnitTests.Models;

public class AffiliationTests
{
    [Fact]
    public void Affiliation_WithValidData_CreatesSuccessfully()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var affiliation = new Affiliation
        {
            Id = id,
            UserId = userId,
            OrganizationId = organizationId
        };

        Assert.Equal(id, affiliation.Id);
        Assert.Equal(userId, affiliation.UserId);
        Assert.Equal(organizationId, affiliation.OrganizationId);
    }

    [Fact]
    public void Affiliation_WithDefaultUser_InitializesNewInstance()
    {
        var affiliation = new Affiliation();
        Assert.NotNull(affiliation.User);
    }

    [Fact]
    public void Affiliation_WithDefaultOrganization_InitializesNewInstance()
    {
        var affiliation = new Affiliation();
        Assert.NotNull(affiliation.Organization);
    }
}
