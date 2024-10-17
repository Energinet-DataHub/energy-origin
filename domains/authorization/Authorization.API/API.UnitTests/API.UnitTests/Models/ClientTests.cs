using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class ClientTests
{
    [Fact]
    public void Client_WithValidData_CreatesSuccessfully()
    {
        var idpClientId = new IdpClientId(Guid.NewGuid());
        var clientType = ClientType.External;

        var client = Client.Create(idpClientId, new ClientName("Client"), clientType, "https://redirect.url");

        client.Should().NotBeNull();
        client.Id.Should().NotBeEmpty();
        client.IdpClientId.Should().Be(idpClientId);
        client.ClientType.Should().Be(clientType);
    }
}
