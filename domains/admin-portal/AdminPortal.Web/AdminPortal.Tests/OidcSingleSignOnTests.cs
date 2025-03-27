using System.Net;
using System.Threading.Tasks;
using AdminPortal.Tests.Setup;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdminPortal.Tests;

public class OidcSingleSignOnTests
{
    [Fact]
    public async Task Given_UserIsNotAuthenticated_When_AccessingAdminPortal_Then_Return401Unauthorized()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ett-admin-portal/ActiveContracts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_UserIsAuthenticated_When_AccessingAdminPortal_Then_Return200OK()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient<GeneralUser>(new WebApplicationFactoryClientOptions(), 12345);

        var response = await client.GetAsync("/ett-admin-portal/ActiveContracts", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Active Contracts", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
