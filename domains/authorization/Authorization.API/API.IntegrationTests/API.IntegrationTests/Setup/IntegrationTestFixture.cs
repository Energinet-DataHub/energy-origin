using API.Models;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;
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
    public PostgresContainer PostgresContainer { get; } = new();
    private RabbitMqContainer RabbitMqContainer { get; } = new();
    private Respawner? _respawner;
    public TestWebApplicationFactory WebAppFactory { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        await RabbitMqContainer.InitializeAsync();

        var newDatabase = await PostgresContainer.CreateNewDatabase();

        var rabbitMqOptions = RabbitMqContainer.Options;

        WebAppFactory.ConnectionString = newDatabase.ConnectionString;
        WebAppFactory.SetRabbitMqOptions(rabbitMqOptions);
        await WebAppFactory.InitializeAsync();

        await using var connection = new NpgsqlConnection(WebAppFactory.ConnectionString);
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


    public async ValueTask DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("Respawner not initialized yet.");
        if (WebAppFactory.ConnectionString is null)
            throw new InvalidOperationException("No test database was created.");

        await using var connection = new NpgsqlConnection(WebAppFactory.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(WebAppFactory.ConnectionString);
        await using var dbContext = new ApplicationDbContext(optionsBuilder.Options);
        var requiredTypes = Enum.GetValues<TermsType>();
        foreach (var type in requiredTypes)
        {
            if (!await dbContext.Terms.AnyAsync(t => t.Type == type))
            {
                dbContext.Terms.Add(Terms.Create(1, type));
            }
        }
        await dbContext.SaveChangesAsync();
    }
}
