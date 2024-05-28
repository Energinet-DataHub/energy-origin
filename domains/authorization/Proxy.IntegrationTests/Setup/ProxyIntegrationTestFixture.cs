using Proxy.IntegrationTests.Swagger;

namespace Proxy.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<ProxyIntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class ProxyIntegrationTestFixture : IDisposable
{
    public ProxyWebApplicationFactory Factory { get; private set; } = new();
    public SwaggerWebApplicationFactory SwaggerFactory { get; private set; } = new();

    public void Dispose()
    {

    }
}
