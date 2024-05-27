namespace Proxy.IntegrationTests;

public class IntegrationTestFixture : IDisposable
{
    public CustomWebApplicationFactory Factory { get; private set; }

    public IntegrationTestFixture()
    {
        Factory = new CustomWebApplicationFactory();
    }

    public void Dispose()
    {

    }
}
