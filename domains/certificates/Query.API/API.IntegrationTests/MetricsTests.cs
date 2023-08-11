using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

public class MetricsTests : TestBase, IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory apiFactory;
    private readonly RegistryConnectorApplicationFactory connectorFactory;

    public MetricsTests(QueryApiWebApplicationFactory apiFactory, RegistryConnectorApplicationFactory connectorFactory)
    {
        this.apiFactory = apiFactory;
        this.connectorFactory = connectorFactory;
    }

    [Fact]
    public async Task api_has_metrics_endpoint()
    {
        using var client = apiFactory.CreateClient();
        using var response = await client.GetAsync("metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task connector_has_metrics_endpoint()
    {
        using var client = connectorFactory.CreateClient();
        using var response = await client.GetAsync("metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
