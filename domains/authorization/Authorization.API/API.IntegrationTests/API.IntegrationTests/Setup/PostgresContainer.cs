using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace API.IntegrationTests.Setup;

public class PostgresContainer : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();

    public async Task<DatabaseInfo> CreateNewDatabase()
    {
        var randomName = Guid.NewGuid().ToString().Substring(0, 8);
        await Container.ExecScriptAsync("CREATE DATABASE " + randomName);
        var regex = new Regex("Database=[^;]+;");
        var match = regex.Match(ConnectionString);
        return new DatabaseInfo(ConnectionString.Replace(match.Value, "Database=" + randomName + ";"));
    }
}

public class DatabaseInfo
{
    public string ConnectionString { get; private set; }

    public DatabaseInfo(string connectionString)
    {
        ConnectionString = connectionString;
    }
}

