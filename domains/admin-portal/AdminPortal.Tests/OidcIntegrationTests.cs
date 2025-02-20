using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdminPortal.Tests;

public class OidcIntegrationTests
{
    [Fact]
    public async Task UnauthenticatedUser_CannotAccessAdminPortal()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ett-admin-portal/ActiveContracts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_CanAccessAdminPortal()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateLoggedInClient<GeneralUser>(new WebApplicationFactoryClientOptions(), 12345);

        var response = await client.GetAsync("/ett-admin-portal/ActiveContracts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("12345", body);
    }
}
