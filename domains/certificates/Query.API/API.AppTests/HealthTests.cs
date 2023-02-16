using System.Net;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using API.IntegrationTest.Infrastructure;
using API.RabbitMq.Configurations;
using Xunit;

namespace API.AppTests;

public class HealthTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<RabbitMqContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public HealthTests(QueryApiWebApplicationFactory factory, RabbitMqContainer rabbitMqContainer)
    {
        this.factory = factory;
        this.factory.RabbitMqSetup = new RabbitMqOptions
        {
            Username = rabbitMqContainer.Username,
            Password = rabbitMqContainer.Password,
            Host = rabbitMqContainer.Hostname,
            Port = rabbitMqContainer.Port
        };
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        var client = factory.CreateClient();
        var healthResponse = await client.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
