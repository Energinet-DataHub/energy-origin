namespace AccessControl.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public AccessControlWebApplicationFactory WebAppFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        WebAppFactory = new AccessControlWebApplicationFactory();
        await WebAppFactory.InitializeAsync();
    }
    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
    }
}
