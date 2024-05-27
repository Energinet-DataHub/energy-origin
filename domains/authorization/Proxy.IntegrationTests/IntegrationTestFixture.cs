namespace Proxy.IntegrationTests;

public class IntegrationTestFixture : IDisposable
{
    public HttpClient Client { get; private set; }
    private readonly CustomWebApplicationFactory _factory;

    public IntegrationTestFixture()
    {
        _factory = new CustomWebApplicationFactory();

        Client = _factory.CreateAuthenticatedClient();
    }

    public void Dispose()
    {
        Client?.Dispose();
    }
}
