namespace API.IntegrationTests.Setup;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;
}
