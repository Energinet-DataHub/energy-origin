extern alias registryConnector;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using ProjectOrigin.HierarchicalDeterministicKeys;
using registryConnector::RegistryConnector.Worker;
using Xunit;

namespace API.IntegrationTests;

public class RegistryConnectorMetricsTests : IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly RegistryConnectorApplicationFactory factory;

    public RegistryConnectorMetricsTests(RegistryConnectorApplicationFactory factory)
    {
        this.factory = factory;
        //TODO: This is a hack to get the test to run. Better option is to use the testcontainers for projectorigin stack
        factory.ProjectOriginOptions = new ProjectOriginOptions
        {
            RegistryName = "foo",
            RegistryUrl = "bar",
            WalletUrl = "baz",
            Dk1IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().ExportPkixText()),
            Dk2IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().ExportPkixText())
        };
    }

    [Fact]
    public async Task has_metrics_endpoint()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
