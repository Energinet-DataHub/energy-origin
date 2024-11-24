using System.Data.Common;
using API.Models;
using API.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;

namespace API.IntegrationTests.Setup;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFixture>
{
    protected readonly IntegrationTestFixture _fixture;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;
    private Respawner _respawner = null!;
    private DbConnection _connection = null!;
    public PostgresContainer PostgresContainer = null!;

    private IServiceScope _scope = null!;
    public ApplicationDbContext DbContext => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    private DatabaseInfo _databaseInfo = null!;

    public async Task InitializeAsync()
    {
        PostgresContainer = new PostgresContainer();
        await PostgresContainer.InitializeAsync();
        _databaseInfo = await PostgresContainer.CreateNewDatabase();

        await SharedRabbitMqContainer.Instance.InitializeAsync();

        var rabbitMqOptions = new RabbitMqOptions
        {
            Host = "localhost",
            Port = 5672,
            Username = "guest",
            Password = "guest"
        };

        WebAppFactory = new TestWebApplicationFactory
        {
            ConnectionString = _databaseInfo.ConnectionString
        };

        WebAppFactory.SetRabbitMqOptions(rabbitMqOptions);
        await WebAppFactory.InitializeAsync();

        _scope = WebAppFactory.Services.CreateScope();
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _connection = dbContext.Database.GetDbConnection();
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new[] { new Table("public", "Terms") }
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connection);

        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await dbContext.Terms.AnyAsync())
        {
            dbContext.Terms.Add(Terms.Create(version: 1));
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await _connection.CloseAsync();

        if (_scope is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _scope.Dispose();
        }

        await WebAppFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
    }
}
