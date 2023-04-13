using System.Net;
using System.Threading.Tasks;
using API.AppTests.Infrastructure.Attributes;
using API.AppTests.Infrastructure.Factories;
using Xunit;

namespace API.AppTests;

[WriteToConsole]
public class UnhealthTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public UnhealthTests(QueryApiWebApplicationFactory factory)
    {
        this.factory = factory;
        this.factory.RabbitMqOptions = null;
    }

    [Fact]
    public async Task Health_IsCalledWhenRabbitMqIsDown_ReturnsServiceUnavailable()
    {
        using var client = factory.CreateClient();
        using var healthResponse = await client.GetAsync("health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
    }
}
