using System.Net.Http;
using System.Threading.Tasks;
using Proxy.IntegrationTests.Testcontainers;
using Xunit;

namespace Proxy.IntegrationTests
{
    public class ProjectOriginStackTests : IAsyncLifetime
    {
        private readonly ProjectOriginStack _projectOriginStack;

        public ProjectOriginStackTests()
        {
            _projectOriginStack = new ProjectOriginStack();
        }

        [Fact]
        public async Task WalletContainer_Should_RespondToCertificatesEndpoint()
        {
            // Arrange
            var walletUrl = _projectOriginStack.WalletUrl;
            var certificatesEndpoint = $"{walletUrl}v1/certificates";

            // Act
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(certificatesEndpoint);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        public async Task InitializeAsync()
        {
            await _projectOriginStack.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await _projectOriginStack.DisposeAsync();
        }
    }
}
