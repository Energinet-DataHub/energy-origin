using API.IntegrationTests.Factories;
using System.Net;
using System.Threading.Tasks;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests;

public class RegistryConnectorHealthTests :
    TestBase,
    IClassFixture<RegistryConnectorApplicationFactory>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<PostgresContainer>
{
    private readonly RegistryConnectorApplicationFactory factory;

    public RegistryConnectorHealthTests(RegistryConnectorApplicationFactory factory, RabbitMqContainer rabbitMqContainer, PostgresContainer dbContainer)
    {
        this.factory = factory;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
        this.factory.ConnectionString = dbContainer.ConnectionString;
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        using var client = factory.CreateClient();
        using var healthResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
