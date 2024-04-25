using API.Models;

namespace API.UnitTests.Models;

public class UserTests
{
    [Fact]
    public void User_WithValidData_CreatesSuccessfully()
    {
        var id = Guid.NewGuid();
        var idpId = "idpId";
        var idpUserId = "idpUserId";
        var name = "Test User";

        var user = new User
        {
            Id = id,
            IdpId = idpId,
            IdpUserId = idpUserId,
            Name = name
        };

        Assert.Equal(id, user.Id);
        Assert.Equal(idpId, user.IdpId);
        Assert.Equal(idpUserId, user.IdpUserId);
        Assert.Equal(name, user.Name);
    }

    [Fact]
    public void User_WithEmptyAffiliations_InitializesEmptyList()
    {
        var user = new User();
        Assert.Empty(user.Affiliations);
    }

    [Fact]
    public void User_WithAffiliations_InitializesCorrectly()
    {
        var organization = new Organization();
        var user = new User
        {
            Affiliations = new List<Affiliation>
            {
                new(new User(), organization) { Id = Guid.NewGuid() }
            }
        };

        Assert.Equal(user.Affiliations.First().User, user);
        Assert.Equal(user.Affiliations.First().Organization, organization);
    }
}
