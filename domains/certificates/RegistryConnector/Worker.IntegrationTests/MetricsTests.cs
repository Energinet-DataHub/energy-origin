using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Worker.IntegrationTests.Factories;
using Xunit;

namespace Worker.IntegrationTests;

public class MetricsTests : IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly RegistryConnectorApplicationFactory factory;

    public MetricsTests(RegistryConnectorApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task has_metrics_endpoint()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
