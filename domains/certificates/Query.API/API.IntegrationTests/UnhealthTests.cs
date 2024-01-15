using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

public class UnhealthTests : TestBase, IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly RegistryConnectorApplicationFactory connectorFactory;

    public UnhealthTests(QueryApiWebApplicationFactory factory, RegistryConnectorApplicationFactory connectorFactory)
    {
        this.factory = factory;
        this.connectorFactory = connectorFactory;
        this.connectorFactory.RabbitMqOptions = null;
        this.connectorFactory.ConnectionString = "Server=foo;Port=5432;Database=bar;User Id=baz;Password=qux;";
    }

    [Fact]
    public async Task Health_IsCalledWhenRabbitMqIsDown_ReturnsServiceUnavailable()
    {
        using var client = connectorFactory.CreateClient();
        using var healthResponse = await client.GetAsync("health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
