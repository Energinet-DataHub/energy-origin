using API.Configuration;
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
    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder().WithUsername("guest").WithPassword("guest").Build();
    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        await RabbitMqContainer.StartAsync();
        var newDatabase = await PostgresContainer.CreateNewDatabase();

        var connectionStringSplit = RabbitMqContainer.GetConnectionString().Split(":");
        var rabbitMqOptions = new RabbitMqOptions
        {
            Host = connectionStringSplit[0],
            Port = int.Parse(connectionStringSplit[^1].TrimEnd('/')),
            Username = "guest",
            Password = "guest"
        };

        WebAppFactory = new TestWebApplicationFactory();
        WebAppFactory.ConnectionString = newDatabase.ConnectionString;
        WebAppFactory.SetRabbitMqOptions(rabbitMqOptions);
        await WebAppFactory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
    }
}
