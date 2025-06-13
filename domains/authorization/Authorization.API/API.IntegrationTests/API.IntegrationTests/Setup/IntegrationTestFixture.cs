using EnergyTrackAndTrace.Testing.Testcontainers;
using Npgsql;

namespace API.IntegrationTests.Setup;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly DatabaseServerFixture _dbServer =
        DatabaseServerFixture.Instance ?? throw new InvalidOperationException("DatabaseServerFixture was not initialised");

    private readonly RabbitMqContainer _rabbit = new();

    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _rabbit.InitializeAsync();

        var newDbName = $"t{Guid.NewGuid():N}";
        await using (var conn = new NpgsqlConnection(_dbServer.BaseConnection))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                CREATE DATABASE "{newDbName}"
                TEMPLATE "{_dbServer.TemplateDbName}";
            """;
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = _dbServer.BaseConnection
            .Replace("Database=postgres", $"Database={newDbName}");

        WebAppFactory = new TestWebApplicationFactory
        {
            ConnectionString = ConnectionString
        };
        WebAppFactory.SetRabbitMqOptions(_rabbit.Options);
        await WebAppFactory.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await WebAppFactory.DisposeAsync();
        }
        finally
        {
            await _rabbit.DisposeAsync();

            await using var conn = new NpgsqlConnection(_dbServer.BaseConnection);
            await conn.OpenAsync();
            var dbName = new NpgsqlConnectionStringBuilder(ConnectionString).Database;
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""DROP DATABASE IF EXISTS "{dbName}";""";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}
