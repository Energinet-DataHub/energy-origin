namespace API.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgresContainer PostgresContainer { get; } = new();
    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        var newDatabase = PostgresContainer.CreateNewDatabase().Result;

        WebAppFactory = new TestWebApplicationFactory();
        WebAppFactory.ConnectionString = newDatabase.ConnectionString;
        await WebAppFactory.InitializeAsync();
    }
    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
    }
}
