using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class HealthControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory _factory;
    public HealthControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        _factory = integrationTestFixture.Factory;
    }

    [Fact]
    public async Task Health_ShouldReturnOk_WhenStarted()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
