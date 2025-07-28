using System.Threading.Tasks;
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

    public ValueTask InitializeAsync()
    {
        Factory.WalletBaseUrl = WalletWireMockServer.Url!;
        Factory.Start();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        WalletWireMockServer.Stop();
        WalletWireMockServer.Dispose();
        await Factory.DisposeAsync();

    }
}
