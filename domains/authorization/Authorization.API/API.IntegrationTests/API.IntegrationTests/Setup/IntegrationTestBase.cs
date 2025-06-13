namespace API.IntegrationTests.Setup;

[Collection(IntegrationTestCollection.CollectionName)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
        => Fixture = fixture;

    public virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
