using Microsoft.AspNetCore.Http;
using WireMock.Server;

namespace Proxy.IntegrationTests;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = "IntegrationTestCollection";
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public ProxyWebApplicationFactory Factory { get; private set; }
    public WireMockServer WalletWireMockServer { get; private set; }

    public IntegrationTestFixture()
    {
        Factory = new ProxyWebApplicationFactory();
        WalletWireMockServer = WireMockServer.Start();
    }

    public async Task InitializeAsync()
    {

        Factory.WalletBaseUrl = WalletWireMockServer.Url!;
        Factory.Start();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}
