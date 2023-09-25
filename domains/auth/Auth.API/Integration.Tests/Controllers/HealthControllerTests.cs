using System.Net;

namespace Integration.Tests.Controllers;

public class HealthControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public HealthControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Health_ShouldReturnOk_WhenStarted()
    {
        var response = await factory.CreateAnonymousClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
