using API.Models;

namespace API.UnitTests.Models;

public class ConsentTests
{
    [Fact]
    public void Consent_WithValidData_CreatesSuccessfully()
    {
        var id = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var consentDate = DateTime.UtcNow;

        var consent = new Consent
        {
            Id = id,
            OrganizationId = organizationId,
            ClientId = clientId,
            ConsentDate = consentDate
        };

        Assert.Equal(id, consent.Id);
        Assert.Equal(organizationId, consent.OrganizationId);
        Assert.Equal(clientId, consent.ClientId);
        Assert.Equal(consentDate, consent.ConsentDate);
    }

    [Fact]
    public void Consent_WithDefaultOrganization_InitializesNewInstance()
    {
        var consent = new Consent();
        Assert.NotNull(consent.Organization);
    }

    [Fact]
    public void Consent_WithDefaultClient_InitializesNewInstance()
    {
        var consent = new Consent();
        Assert.NotNull(consent.Client);
    }
}
