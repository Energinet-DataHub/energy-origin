using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class PostgresContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer;

    public PostgresContainer() => testContainer = new PostgreSqlBuilder().WithImage("postgres:15.2")
        .Build();

    public string ConnectionString => testContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();
    }

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();

    public async Task<DatabaseInfo> CreateNewDatabase()
    {
        var randomName = "a" + Guid.NewGuid().ToString().Substring(0, 8);
        await testContainer.ExecScriptAsync("CREATE DATABASE " + randomName);
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
