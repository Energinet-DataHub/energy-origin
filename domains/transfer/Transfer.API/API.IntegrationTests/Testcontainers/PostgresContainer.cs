using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class PostgresContainer
{
    private static readonly Lazy<PostgresContainer> lazyContainer = new Lazy<PostgresContainer>(() =>
    {
        var container = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();
        container.StartAsync().GetAwaiter().GetResult();
        return new PostgresContainer(container);
    });

    private readonly PostgreSqlContainer _container;

    private PostgresContainer(PostgreSqlContainer container)
    {
        _container = container;
    }

    public static PostgresContainer Instance => lazyContainer.Value;

    public string ConnectionString => _container.GetConnectionString();

    public async Task<DatabaseInfo> CreateNewDatabase()
    {
        var randomName = "a" + Guid.NewGuid().ToString().Substring(0, 8);
        await _container.ExecScriptAsync("CREATE DATABASE " + randomName);
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
