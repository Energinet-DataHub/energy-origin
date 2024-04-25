using API.Models;

namespace API.UnitTests.Models;

public class ConsentTests
{
    [Fact]
    public void Consent_WithValidData_CreatesSuccessfully()
    {
        var id = Guid.NewGuid();
        var organization = new Organization();
        var client = new Client();
        var consentDate = DateTime.UtcNow;

        var consent = new Consent(organization, client)
        {
            Id = id,
            ConsentDate = consentDate
        };

        Assert.Equal(id, consent.Id);
        Assert.Equal(organization, consent.Organization);
        Assert.Equal(client, consent.Client);
        Assert.Equal(consentDate, consent.ConsentDate);
    }

    [Fact]
    public void Consent_WithDefaultOrganization_InitializesNewInstance()
    {
        var organization = new Organization();
        var client = new Client();
        var consent = new Consent(organization, client);

        Assert.NotNull(consent.Organization);
    }

    [Fact]
    public void Consent_WithDefaultClient_InitializesNewInstance()
    {
        var organization = new Organization();
        var client = new Client();
        var consent = new Consent(organization, client);

        Assert.NotNull(consent.Client);
    }
}
