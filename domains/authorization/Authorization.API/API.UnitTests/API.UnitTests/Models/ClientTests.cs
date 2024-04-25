using API.Models;

namespace API.UnitTests.Models;

public class ClientTests
{
    [Fact]
    public void Client_WithValidData_CreatesSuccessfully()
    {
        var id = Guid.NewGuid();
        var idpClientId = "idpClientId";
        var name = "Test Client";
        var role = Role.External;

        var client = new Client
        {
            Id = id,
            IdpClientId = idpClientId,
            Name = name,
            Role = role
        };

        Assert.Equal(id, client.Id);
        Assert.Equal(idpClientId, client.IdpClientId);
        Assert.Equal(name, client.Name);
        Assert.Equal(role, client.Role);
    }

    [Fact]
    public void Client_WithEmptyIdpClientId_InitializesEmptyString()
    {
        var client = new Client();
        Assert.Equal(string.Empty, client.IdpClientId);
    }

    [Fact]
    public void Client_WithEmptyName_InitializesEmptyString()
    {
        var client = new Client();
        Assert.Equal(string.Empty, client.Name);
    }
}
