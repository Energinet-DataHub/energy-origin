using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class PostgresContainer : IAsyncLifetime
{
    public PostgreSqlContainer TestContainer { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:15") // We use this version in production
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .WithCommand("postgres", "-c", "fsync=off", "-c", "synchronous_commit=off", "-c", "full_page_writes=off") // Faster I/O for testing
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready").UntilPortIsAvailable(5432))
        .WithPortBinding(PostgreSqlBuilder.PostgreSqlPort, true)
        .Build();

    public string ConnectionString => TestContainer.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await TestContainer.StartAsync();
    }

    public ValueTask DisposeAsync() => TestContainer.DisposeAsync();

    public async Task<DatabaseInfo> CreateNewDatabase()
    {
        var randomName = "a" + Guid.NewGuid().ToString().Substring(0, 8); // must start with a letter or underscore
        await TestContainer.ExecScriptAsync("CREATE DATABASE " + randomName);
        var regex = new Regex("Database=[^;]+;");
        var match = regex.Match(ConnectionString);
        return new DatabaseInfo(ConnectionString.Replace(match.Value, "Database=" + randomName + ";"));
    }
}

public class DatabaseInfo
{
    public string ConnectionString { get; private set; }

    public DatabaseInfo(string connectionString) => ConnectionString = connectionString;

    public string Host => GetValue("Host");
    public string Port => GetValue("Port");
    public string Name => GetValue("Database");
    public string User => GetValue("Username");
    public string Password => GetValue("Password");

    private string GetValue(string key)
    {
        var regex = new Regex(key + "=[^;]+;");
        var match = regex.Match(ConnectionString + ";");
        return match.Value.Substring(key.Length + 1 /* avoid '=' */, match.Value.Length - key.Length - 2 /* avoid '=' and ';' */);
    }
}
