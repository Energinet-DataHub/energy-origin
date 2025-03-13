using EnergyTrackAndTrace.Testing.Testcontainers;
using Respawn;
using Npgsql;
using Respawn.Graph;

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

    private DatabaseInfo? _databaseInfo;

    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        await ProjectOriginStack.InitializeAsync();
        await RabbitMqContainer.InitializeAsync();

        _databaseInfo = await PostgresContainer.CreateNewDatabase();

        WebAppFactory = new TestWebApplicationFactory
        {
            WalletUrl = ProjectOriginStack.WalletUrl,
            ConnectionString = _databaseInfo.ConnectionString,
        };
        WebAppFactory.SetRabbitMqOptions(RabbitMqContainer.Options);
        await WebAppFactory.InitializeAsync();

        await using var connection = new NpgsqlConnection(_databaseInfo.ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = new Table[]
            {
                "__EFMigrationsHistory",
                "Terms"
            },
            DbAdapter = DbAdapter.Postgres,
        });
    }

    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await ProjectOriginStack.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("Respawner not initialized yet.");

        if (_databaseInfo is null)
            throw new InvalidOperationException("No test database was created.");

        await using var connection = new NpgsqlConnection(_databaseInfo.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}
