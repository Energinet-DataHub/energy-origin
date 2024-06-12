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
    public WireMockServer WalletWireMockServer { get; private set; } = WireMockServer.Start();

    public Task InitializeAsync()
    {
        Factory.WalletBaseUrl = WalletWireMockServer.Url!;
        Factory.Start();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        WalletWireMockServer.Stop();
        WalletWireMockServer.Dispose();
        await Factory.DisposeAsync();

    }
}
