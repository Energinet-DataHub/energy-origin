using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class PostgresContainer
{
    private static PostgresContainer? _instance;
    private readonly PostgreSqlContainer _container;

    private PostgresContainer()
    {
        _container = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();
        _container.StartAsync().GetAwaiter().GetResult();
    }

    public static PostgresContainer GetInstance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PostgresContainer();
            }
            return _instance;
        }
    }

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
