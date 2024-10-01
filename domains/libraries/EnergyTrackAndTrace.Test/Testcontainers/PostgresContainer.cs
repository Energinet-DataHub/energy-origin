using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace EnergyTrackAndTrace.Test.Testcontainers;

public class PostgresContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer;

    public PostgresContainer() => testContainer = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();

    public string ConnectionString => testContainer.GetConnectionString();

    public async Task InitializeAsync() => await testContainer.StartAsync();

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();

    public async Task<DatabaseInfo> CreateNewDatabase()
    {
        var randomName = Guid.NewGuid().ToString().Substring(0, 8);
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
}
