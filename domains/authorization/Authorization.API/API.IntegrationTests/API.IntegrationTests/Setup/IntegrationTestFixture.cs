using EnergyTrackAndTrace.Testing.Testcontainers;

namespace API.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgresDatabase PostgresDatabase { get; } = new();
    public ProjectOriginStack ProjectOriginStack { get; } = new();

    public RabbitMqContainer RabbitMqContainer { get; } = new();

    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await PostgresDatabase.InitializeAsync();
        await ProjectOriginStack.InitializeAsync();
        await RabbitMqContainer.InitializeAsync();

        var newDatabase = await PostgresDatabase.CreateNewDatabase();

        var rabbitMqOptions = RabbitMqContainer.Options;

        WebAppFactory = new TestWebApplicationFactory();
        WebAppFactory.WalletUrl = ProjectOriginStack.WalletUrl;
        WebAppFactory.ConnectionString = newDatabase.ConnectionString;
        WebAppFactory.SetRabbitMqOptions(rabbitMqOptions);
        await WebAppFactory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresDatabase.DisposeAsync();
        await ProjectOriginStack.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
    }
}
