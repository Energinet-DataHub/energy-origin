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
        var name = new Name("Test Client");
        var role = Role.External;

        var client = Client.Create(idpClientId, name, role);

        client.Should().NotBeNull();
        client.Id.Should().NotBeEmpty();
        client.IdpClientId.Should().Be(idpClientId);
        client.Name.Should().Be(name);
        client.Role.Should().Be(role);
    }

    [Fact]
    public void Client_CanExist_WithoutConsents()
    {
        var idpClientId = new IdpClientId(Guid.NewGuid());
        var name = new Name("Test Client");
        var role = Role.External;

        var client = Client.Create(idpClientId, name, role);

        client.Consents.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Client_CanHave_Consents()
    {
        var idpClientId = new IdpClientId(Guid.NewGuid());
        var name = new Name("Test Client");
        var role = Role.External;

        var organizationIdpId = new IdpId(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");

        var organization = Organization.Create(organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        var client = Client.Create(idpClientId, name, role);
        var consentDate = DateTime.UtcNow;
        var consent = Consent.Create(organization, client, consentDate);

        client.Consents.Should().Contain(consent);
    }
}