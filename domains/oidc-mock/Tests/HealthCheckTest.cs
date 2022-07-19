using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Tests;

public class HealthCheckTest
{
    [Fact]
    public async Task IsHealthy()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
