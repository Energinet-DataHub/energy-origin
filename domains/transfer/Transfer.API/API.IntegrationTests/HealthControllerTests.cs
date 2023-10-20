using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using Xunit;

namespace API.IntegrationTests;

public class HealthControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    public HealthControllerTests(TransferAgreementsApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Health_ShouldReturnOk_WhenStarted()
    {
        var response = await factory.CreateUnauthenticatedClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
