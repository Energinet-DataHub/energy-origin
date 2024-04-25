using API.Models;
using Xunit;

namespace API.UnitTests.Models;

public class ConsentTests
{
    [Fact]
    public void Consent_WithValidData_CreatesSuccessfully()
    {
        var organization = new Organization();
        var client = new Client();
        var consentDate = DateTime.UtcNow;

        var consent = Consent.Create(organization, client, consentDate);

        Assert.Equal(organization, consent.Organization);
        Assert.Equal(client, consent.Client);
        Assert.Equal(consentDate, consent.ConsentDate);
    }

    [Fact]
    public void Consent_WithDefaultOrganization_InitializesNewInstance()
    {
        var organization = new Organization();
        var client = new Client();
        var consent = Consent.Create(organization, client, DateTime.UtcNow);

        Assert.NotNull(consent.Organization);
    }

    [Fact]
    public void Consent_WithDefaultClient_InitializesNewInstance()
    {
        var organization = new Organization();
        var client = new Client();
        var consent = Consent.Create(organization, client, DateTime.UtcNow);

        Assert.NotNull(consent.Client);
    }
}
