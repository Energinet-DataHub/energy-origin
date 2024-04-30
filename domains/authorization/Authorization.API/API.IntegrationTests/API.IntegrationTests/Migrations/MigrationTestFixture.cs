namespace API.IntegrationTests.Migrations;

public class MigrationTestFixture : IAsyncLifetime
{
    public PostgresContainer PostgresContainer { get; } = new();

    public Task InitializeAsync() => PostgresContainer.InitializeAsync();

    public Task DisposeAsync() => PostgresContainer.DisposeAsync();
}

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<MigrationTestFixture>
{
    public const string CollectionName = "Integration test collection";
}
