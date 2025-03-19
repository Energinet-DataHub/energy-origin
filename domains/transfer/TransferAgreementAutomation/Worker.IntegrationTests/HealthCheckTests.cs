using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Worker.IntegrationTest;

public class HealthControllerTests : IClassFixture<TransferAutomationApplicationFactory>
{
    private readonly TransferAutomationApplicationFactory fixture;

    public HealthControllerTests(TransferAutomationApplicationFactory fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Health_ShouldReturnOk_WhenStarted()
    {
        var response = await fixture.CreateUnauthenticatedClient().GetAsync("/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
