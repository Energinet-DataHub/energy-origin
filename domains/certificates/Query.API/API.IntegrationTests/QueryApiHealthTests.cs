using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests;

public class QueryApiHealthTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<PostgresContainer>,
    IClassFixture<RabbitMqContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public QueryApiHealthTests(QueryApiWebApplicationFactory factory, RabbitMqContainer rabbitMqContainer, PostgresContainer dbContainer)
    {
        this.factory = factory;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
        this.factory.ConnectionString = dbContainer.ConnectionString;
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        using var client = factory.CreateClient();
        using var healthResponse = await client.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
