using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class QueryApiHealthTests : TestBase
{
    private readonly QueryApiWebApplicationFactory factory;

    public QueryApiHealthTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.WebApplicationFactory;
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        using var client = factory.CreateClient();
        using var healthResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
