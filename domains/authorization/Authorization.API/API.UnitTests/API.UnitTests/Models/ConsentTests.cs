using API.Models;
using API.ValueObjects;
using FluentAssertions;
/*
namespace API.UnitTests.Models;

public class ConsentTests
{
    [Fact]
    public void Consent_WithValidData_CreatesSuccessfully()
    {
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationTin, organizationName);

        var clientIdpClientId = new IdpClientId(Guid.NewGuid());
        var role = ClientType.External;
        var client = Client.Create(clientIdpClientId, new ClientName("Client"), role, "https://redirect.url");

        var consentDate = DateTimeOffset.UtcNow;
        var consent = Consent.Create(organization, client, consentDate);

        consent.Should().NotBeNull();
        consent.Organization.Should().Be(organization);
        consent.Client.Should().Be(client);
        consent.ConsentDate.Should().Be(consentDate);
        consent.OrganizationId.Should().Be(organization.Id);
        consent.ClientId.Should().Be(client.Id);
    }

    [Fact]
    public void Consent_Create_AddsConsentToOrganizationAndClient()
    {
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationTin, organizationName);

        var clientIdpClientId = new IdpClientId(Guid.NewGuid());
        var role = ClientType.External;
        var client = Client.Create(clientIdpClientId, new ClientName("Client"), role, "https://redirect.url");

        var consentDate = DateTimeOffset.UtcNow;
        var consent = Consent.Create(organization, client, consentDate);

        organization.Consents.Should().Contain(consent);
        client.Consents.Should().Contain(consent);
    }

    [Fact]
    public void Consent_Create_ThrowsArgumentNullException_WhenOrganizationIsNull()
    {
        var clientIdpClientId = new IdpClientId(Guid.NewGuid());
        var role = ClientType.External;
        var client = Client.Create(clientIdpClientId, new ClientName("Client"), role, "https://redirect.url");

        var consentDate = DateTimeOffset.UtcNow;
        Action act = () => Consent.Create(null!, client, consentDate);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Consent_Create_ThrowsArgumentNullException_WhenClientIsNull()
    {
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = Organization.Create(organizationTin, organizationName);

        var consentDate = DateTimeOffset.UtcNow;
        Action act = () => Consent.Create(organization, null!, consentDate);

        act.Should().Throw<ArgumentNullException>();
    }
} TODO
*/
