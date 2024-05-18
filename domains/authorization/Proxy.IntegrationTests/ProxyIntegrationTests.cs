using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class ProxyIntegrationTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task TokenModificationMiddleware_ModifiesToken_WhenOrganizationIdExists()
    {
        var wireMockServer = fixture.WalletWireMockServer;
        wireMockServer.Given(
            Request.Create().WithPath("/wallet-api/v1/test").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200)
        );

        var client = fixture.Factory.CreateAuthenticatedClient("123");

        var response = await client.GetAsync("/wallet-api/v1/test?organizationId=123");

        Assert.True(response.IsSuccessStatusCode);
        var requests = wireMockServer.LogEntries;
        var requestHeader = requests.First().RequestMessage.Headers!["Authorization"];
        Assert.Equal("Bearer newToken", requestHeader);
    }
}
