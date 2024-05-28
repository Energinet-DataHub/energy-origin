using Proxy.IntegrationTests.Swagger;
using WireMock.Server;

namespace Proxy.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<ProxyIntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class ProxyIntegrationTestFixture : IAsyncLifetime
{
    public ProxyWebApplicationFactory Factory { get; private set; } = new();
    public SwaggerWebApplicationFactory SwaggerFactory { get; private set; } = new();

    public WireMockServer CvrWireMockServer { get; private set; }


    public ProxyIntegrationTestFixture()
    {
        Factory = new ProxyWebApplicationFactory();
        CvrWireMockServer = WireMockServer.Start();
    }

    public async Task InitializeAsync()
    {
        Factory.WalletBaseUrl = CvrWireMockServer.Url!;
        Factory.Start();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}
