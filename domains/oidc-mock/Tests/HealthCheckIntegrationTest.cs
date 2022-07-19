using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Tests;

public class HealthCheckIntegrationTest
{
    [Fact]
    public async Task IsHealthy()
    {
        var client = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Test"))
            .CreateClient();
        var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
