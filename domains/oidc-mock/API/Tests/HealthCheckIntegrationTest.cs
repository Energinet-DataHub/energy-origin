using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Oidc.Mock;
using Xunit;

namespace Tests;

public class HealthCheckIntegrationTest
{
    [Fact]
    public async Task IsHealthy()
    {
        var client = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Test")
                .UseSetting(Configuration.UsersFilePathKey, "test-users.json"))
            .CreateClient();
        var healthResponse = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
