using EnergyTrackAndTrace.Testing.Testcontainers;

namespace API.IntegrationTests.Setup.Fixtures;

public class ProjectOriginStackRequired : IAsyncLifetime
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
