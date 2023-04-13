using System.Net;
using System.Threading.Tasks;
using API.AppTests.Infrastructure.Factories;
using API.AppTests.Infrastructure.TestBase;
using API.AppTests.Infrastructure.Testcontainers;
using Xunit;

namespace API.AppTests;

public class HealthTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<RabbitMqContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public HealthTests(QueryApiWebApplicationFactory factory, RabbitMqContainer rabbitMqContainer)
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
