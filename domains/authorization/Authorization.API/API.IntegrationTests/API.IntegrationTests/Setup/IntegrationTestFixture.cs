using EnergyTrackAndTrace.Testing.Testcontainers;

namespace API.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgresContainer PostgresContainer { get; } = new();
    public ProjectOriginStack ProjectOriginStack { get; } = new();

    public RabbitMqContainer RabbitMqContainer { get; } = new();

    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        await ProjectOriginStack.InitializeAsync();
        await RabbitMqContainer.InitializeAsync();

        var newDatabase = await PostgresContainer.CreateNewDatabase();

        var rabbitMqOptions = RabbitMqContainer.Options;

        WebAppFactory = new TestWebApplicationFactory();
        WebAppFactory.WalletUrl = ProjectOriginStack.WalletUrl;
        WebAppFactory.ConnectionString = newDatabase.ConnectionString;
        WebAppFactory.SetRabbitMqOptions(rabbitMqOptions);
        await WebAppFactory.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await ProjectOriginStack.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
    }
}
