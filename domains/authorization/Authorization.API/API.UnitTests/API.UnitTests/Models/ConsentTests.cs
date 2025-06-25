using API.Models;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class ConsentTests
{
    [Fact]
    public void Consent_WithValidData_CreatesSuccessfully()
    {
        var organizationTin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");
        var organization = Organization.Create(organizationTin, organizationName);

        var clientIdpClientId = new IdpClientId(Guid.NewGuid());
        var role = ClientType.External;
        var organizationWithClient = Any.OrganizationWithClient(client: Client.Create(clientIdpClientId, new ClientName("Client"), role, "https://redirect.url", false));

        var consentDate = DateTimeOffset.UtcNow;
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, consentDate);

        consent.Should().NotBeNull();
        consent.ConsentGiverOrganizationId.Should().Be(organization.Id);
        consent.ConsentReceiverOrganizationId.Should().Be(organizationWithClient.Id);
        consent.ConsentDate.Should().Be(consentDate);
    }

    [Fact]
    public void Consent_Create_ThrowsArgumentException_WhenOrganizationIsEmpty()
    {
        Action act = () => OrganizationConsent.Create(new Guid(), new Guid(), DateTimeOffset.Now);

        act.Should().Throw<ArgumentException>();
    }
}
