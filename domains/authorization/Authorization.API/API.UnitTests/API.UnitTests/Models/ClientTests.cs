using API.Models;
using API.ValueObjects;
using FluentAssertions;
/*
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

    [Fact]
    public void Client_CanExist_WithoutConsents()
    {
        var idpClientId = new IdpClientId(Guid.NewGuid());
        var clientType = ClientType.External;

        var client = Client.Create(idpClientId, new ClientName("Client"), clientType, "https://redirect.url");

        client.Consents.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Client_CanHave_Consents()
    {
        var idpClientId = new IdpClientId(Guid.NewGuid());
        var clientType = ClientType.External;

        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(organizationTin, organizationName);

        var client = Client.Create(idpClientId, new ClientName("Client"), clientType, "https://redirect.url");
        var consentDate = DateTimeOffset.UtcNow;
        var consent = Consent.Create(organization, client, consentDate);

        client.Consents.Should().Contain(consent);
    }
}
*/
