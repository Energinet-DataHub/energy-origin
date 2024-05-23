namespace Proxy.IntegrationTests.Testcontainers;

public class ProjectOriginStackFixture : IAsyncLifetime
{
    public ProjectOriginStack ProjectOriginStack { get; } = new();

    public async Task InitializeAsync()
    {
        await ProjectOriginStack.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await ProjectOriginStack.DisposeAsync();
    }
}
