using System.Net;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using API.AppTests.Infrastructure.WriteToConsole;
using Xunit;

namespace API.AppTests;

[Collection("Startup")]
[WriteToConsole]
public class HealthTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<RabbitMqContainer>
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
        var client = factory.CreateClient();
        var healthResponse = await client.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
