using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace API.AppTests;

public class HealthTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public HealthTests(QueryApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        var client = factory.CreateClient();
        var healthResponse = await client.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
