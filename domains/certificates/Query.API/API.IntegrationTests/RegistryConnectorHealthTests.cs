using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class RegistryConnectorHealthTests : TestBase
{
    private readonly RegistryConnectorApplicationFactory factory;

    public RegistryConnectorHealthTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.RegistryConnectorFactory;
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        using var client = factory.CreateClient();
        using var healthResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
