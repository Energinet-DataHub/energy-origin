using API.Models;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;
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

    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;

    private DatabaseInfo? _databaseInfo;

    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();

        _databaseInfo = await PostgresContainer.CreateNewDatabase();

        WebAppFactory = new TestWebApplicationFactory
        {
            ConnectionString = _databaseInfo.ConnectionString
        };
        await WebAppFactory.InitializeAsync();

        await using var connection = new NpgsqlConnection(_databaseInfo.ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude =
            [
                "public"
            ],
            TablesToIgnore =
            [
                new Table("__EFMigrationsHistory"),
                new Table("Terms"),
                new Table("InboxState"),
                new Table("OutboxMessage"),
                new Table("OutboxState")
            ],
            DbAdapter = DbAdapter.Postgres,
        });
    }

    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
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

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_databaseInfo.ConnectionString);
        using var dbContext = new ApplicationDbContext(optionsBuilder.Options);
        if (!await dbContext.Terms.AnyAsync())
        {
            dbContext.Terms.Add(Terms.Create(1));
            await dbContext.SaveChangesAsync();
        }
    }
}
