using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests
{
    public class ProxyIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public ProxyIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task TokenModificationMiddleware_ModifiesToken_WhenOrganizationIdExists()
        {
            // Arrange
            var wireMockServer = _fixture.WalletWireMockServer;
            wireMockServer.Given(
                Request.Create().WithPath("/wallet-api/v1/test").UsingGet()
            ).RespondWith(
                Response.Create().WithStatusCode(200)
            );

            var client = _fixture.Factory.CreateAuthenticatedClient("123");

            // Act
            var response = await client.GetAsync("/wallet-api/v1/test?organizationId=123");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var requests = wireMockServer.LogEntries;
            var requestHeader = requests.First().RequestMessage.Headers["Authorization"];
            Assert.Equal("Bearer newToken", requestHeader);
        }
    }
}
