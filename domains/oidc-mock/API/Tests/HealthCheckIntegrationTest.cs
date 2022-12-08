using System.Net;
using Oidc.Mock;
using Microsoft.AspNetCore.Mvc.Testing;
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
        var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
