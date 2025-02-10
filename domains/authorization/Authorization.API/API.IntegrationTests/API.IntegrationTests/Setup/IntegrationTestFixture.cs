using EnergyOrigin.Setup.RabbitMq;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Testcontainers.RabbitMq;

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

    public RabbitMqContainer RabbitMqContainer { get; } =
        new RabbitMqBuilder().WithImage("rabbitmq:3.13-management").WithUsername("guest").WithPassword("guest").WithPortBinding(15672, true).Build();

    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        await ProjectOriginStack.InitializeAsync();

        await RabbitMqContainer.StartAsync();
        var newDatabase = await PostgresContainer.CreateNewDatabase();

        var connectionString = RabbitMqContainer.GetConnectionString();
        var rabbitMqOptions = RabbitMqOptions.FromConnectionString(connectionString);

        WebAppFactory = new TestWebApplicationFactory();
        WebAppFactory.WalletUrl = ProjectOriginStack.WalletUrl;
        WebAppFactory.ConnectionString = newDatabase.ConnectionString;
        WebAppFactory.SetRabbitMqOptions(rabbitMqOptions);
        await WebAppFactory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await ProjectOriginStack.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
    }
}
