using System.Data.Common;
using API.Models;
using API.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using Testcontainers.RabbitMq;

namespace API.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public abstract class IntegrationTestBase(IntegrationTestFixture fixture)
    : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    protected readonly IntegrationTestFixture _fixture = fixture;

    public virtual async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgresContainer PostgresContainer { get; } = new();
    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder().WithUsername("guest").WithPassword("guest").Build();

    public TestWebApplicationFactory WebAppFactory { get; private set; } = null!;
    private Respawner _respawner = null!;
    private DbConnection _connection = null!;

    private IServiceScope _scope = null!;
    public ApplicationDbContext DbContext => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        await RabbitMqContainer.StartAsync();
        var newDatabase = await PostgresContainer.CreateNewDatabase();

        var connectionStringSplit = RabbitMqContainer.GetConnectionString().Split(":");
        var rabbitMqOptions = new RabbitMqOptions
        {
            Host = connectionStringSplit[0],
            Port = int.Parse(connectionStringSplit[^1].TrimEnd('/')),
            Username = "guest",
            Password = "guest"
        };

        WebAppFactory = new TestWebApplicationFactory
        {
            ConnectionString = newDatabase.ConnectionString
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
        await RabbitMqContainer.DisposeAsync();
    }
}
