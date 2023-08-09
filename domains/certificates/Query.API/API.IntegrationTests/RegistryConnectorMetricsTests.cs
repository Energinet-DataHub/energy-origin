extern alias registryConnector;
using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

[Collection(nameof(ProjectOriginStackCollection))]
public class RegistryConnectorMetricsTests : IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly RegistryConnectorApplicationFactory factory;

    public RegistryConnectorMetricsTests(RegistryConnectorApplicationFactory factory, ProjectOriginStack projectOriginStack)
    {
        this.factory = factory;
        factory.ProjectOriginOptions = projectOriginStack.Options;
    }

    [Fact]
    public async Task has_metrics_endpoint()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
