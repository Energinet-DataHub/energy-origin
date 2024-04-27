using API.Models;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class ConsentTests
{
    [Fact]
    public void Consent_WithValidData_CreatesSuccessfully()
    {
        var orgId = Guid.NewGuid();
        var organizationIdpId = new IdpId(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = new Organization(orgId, organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        var clientIdpClientId = new IdpClientId(Guid.NewGuid());
        var clientName = new Name("Test Client");
        var role = Role.External;
        var client = Client.Create(clientIdpClientId, clientName, role);

        var consentDate = DateTime.UtcNow;
        var consent = Consent.Create(organization, client, consentDate);

        consent.Should().NotBeNull();
        consent.Id.Should().NotBeEmpty();
        consent.Organization.Should().Be(organization);
        consent.Client.Should().Be(client);
        consent.ConsentDate.Should().Be(consentDate);
        consent.OrganizationId.Should().Be(organization.Id);
        consent.ClientId.Should().Be(client.Id);
    }

    [Fact]
    public void Consent_Create_AddsConsentToOrganizationAndClient()
    {
        var orgId = Guid.NewGuid();
        var organizationIdpId = new IdpId(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = new Organization(orgId, organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        var clientIdpClientId = new IdpClientId(Guid.NewGuid());
        var clientName = new Name("Test Client");
        var role = Role.External;
        var client = Client.Create(clientIdpClientId, clientName, role);

        var consentDate = DateTime.UtcNow;
        var consent = Consent.Create(organization, client, consentDate);

        organization.Consents.Should().Contain(consent);
        client.Consents.Should().Contain(consent);
    }

    [Fact]
    public void Consent_Create_ThrowsArgumentNullException_WhenOrganizationIsNull()
    {
        var clientIdpClientId = new IdpClientId(Guid.NewGuid());
        var clientName = new Name("Test Client");
        var role = Role.External;
        var client = Client.Create(clientIdpClientId, clientName, role);

        var consentDate = DateTime.UtcNow;
        Action act = () => Consent.Create(null!, client, consentDate);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Consent_Create_ThrowsArgumentNullException_WhenClientIsNull()
    {
        var orgId = Guid.NewGuid();
        var organizationIdpId = new IdpId(Guid.NewGuid());
        var organizationIdpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var organizationTin = new Tin("12345678");
        var organizationName = new OrganizationName("Test Organization");
        var organization = new Organization(orgId,organizationIdpId, organizationIdpOrganizationId, organizationTin, organizationName);

        var consentDate = DateTime.UtcNow;
        Action act = () => Consent.Create(organization, null!, consentDate);

        act.Should().Throw<ArgumentNullException>();
    }
}
