namespace API.IntegrationTests.Setup;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
    }

    public virtual async ValueTask InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
    }

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
