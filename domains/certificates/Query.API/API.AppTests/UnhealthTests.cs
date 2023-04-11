using System.Net;
using System.Threading.Tasks;
using API.AppTests.Helpers;
using API.AppTests.Infrastructure;
using API.AppTests.Infrastructure.WriteToConsole;
using Xunit;

namespace API.AppTests;

[Collection("Startup")]
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
        var client = factory.CreateClient();
        var healthResponse = await client.GetAsync("health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
    }
}
