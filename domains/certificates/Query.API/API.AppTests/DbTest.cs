using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Xunit;

namespace API.AppTests;

public class DbTest : IAsyncLifetime
{
    private readonly TestcontainerDatabase postgresqlContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
        //.WithCleanUp(true)
        .WithDatabase(new PostgreSqlTestcontainerConfiguration
        {
            Database = "db",
            Username = "postgres",
            Password = "postgres",
        })
        .WithImage("sibedge/postgres-plv8")
        .Build();

    [Fact]
    public void ExecuteCommand()
    {
        using var connection = new NpgsqlConnection(postgresqlContainer.ConnectionString);
        using var command = new NpgsqlCommand();
        connection.Open();
        command.Connection = connection;
        command.CommandText = "SELECT 1";
        var dataReader = command.ExecuteReader();

        Assert.True(dataReader.HasRows);
    }

    public async Task InitializeAsync()
    {
        await postgresqlContainer.StartAsync();

        var result = await postgresqlContainer.ExecAsync(new[]
        {
            "/bin/sh", "-c",
            "psql -U postgres -c \"CREATE EXTENSION plv8; SELECT extversion FROM pg_extension WHERE extname = 'plv8';\""
        });
    }

    public Task DisposeAsync() => postgresqlContainer.DisposeAsync().AsTask();
}
