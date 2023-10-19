using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Shared.Factories;
using Xunit;

namespace API.IntegrationTests.Shared;

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
