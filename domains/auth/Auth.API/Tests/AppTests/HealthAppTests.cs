using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Tests.AppTests;

public class HealthAppTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory factory;

    public HealthAppTests(AuthApiWebApplicationFactory factory)
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
