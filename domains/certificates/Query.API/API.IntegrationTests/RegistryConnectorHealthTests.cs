using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests;

public class RegistryConnectorHealthTests :
    TestBase,
    IClassFixture<RegistryConnectorApplicationFactory>,
    IClassFixture<RabbitMqContainer>
{
    private readonly RegistryConnectorApplicationFactory factory;

    public RegistryConnectorHealthTests(RegistryConnectorApplicationFactory factory, RabbitMqContainer rabbitMqContainer)
    {
        this.factory = factory;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        using var client = factory.CreateClient();
        using var healthResponse = await client.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
