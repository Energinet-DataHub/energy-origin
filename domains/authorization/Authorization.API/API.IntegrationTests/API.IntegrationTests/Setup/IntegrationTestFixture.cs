using DotNet.Testcontainers.Builders;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Testcontainers.PostgreSql;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace API.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
        .WithCleanUp(true)
        .Build();

    public ProjectOriginStack ProjectOriginStack { get; } = new();
    public RabbitMqContainer RabbitMqContainer { get; } = new();

    public string ConnectionString { get; private set; } = null!;

    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;

    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        ConnectionString = _postgresContainer.GetConnectionString();

        await ProjectOriginStack.InitializeAsync();
        await RabbitMqContainer.InitializeAsync();

        WebAppFactory = new TestWebApplicationFactory
        {
            WalletUrl = ProjectOriginStack.WalletUrl,
            ConnectionString = ConnectionString,
        };
        WebAppFactory.SetRabbitMqOptions(RabbitMqContainer.Options);
        await WebAppFactory.InitializeAsync(); // e.g. runs EF migrations etc.

        using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,

            TablesToIgnore = new Table[]
            {
                "__EFMigrationsHistory",
                "Terms"
            }
        });
    }

    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
        await ProjectOriginStack.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("Respawner not initialized yet.");

        using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }
}
