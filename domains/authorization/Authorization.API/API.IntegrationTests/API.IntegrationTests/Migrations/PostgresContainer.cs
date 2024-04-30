using System.Text.RegularExpressions;
using Testcontainers.PostgreSql;

namespace API.IntegrationTests.Migrations;

public class PostgresContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();

    private string ConnectionString => testContainer.GetConnectionString();

    public async Task InitializeAsync() => await testContainer.StartAsync();

    public async Task DisposeAsync() => await testContainer.DisposeAsync();

    public async Task<DatabaseInfo> CreateNewDatabase()
    {
        var randomName = Guid.NewGuid().ToString().Substring(0, 8);
        await testContainer.ExecScriptAsync("CREATE DATABASE " + randomName);
        var regex = new Regex("Database=[^;]+;");
        var match = regex.Match(ConnectionString);
        return new DatabaseInfo(ConnectionString.Replace(match.Value, "Database=" + randomName + ";"));
    }
}

public class DatabaseInfo(string connectionString)
{
    public string ConnectionString { get; private set; } = connectionString;
}
