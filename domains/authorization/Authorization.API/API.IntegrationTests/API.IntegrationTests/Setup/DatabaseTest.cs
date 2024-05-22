using API.Data;
using API.Models;
using API.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;

namespace API.IntegrationTests.Setup;

public abstract class DatabaseTest : IAsyncLifetime
{
    public readonly ApplicationDbContext Db;
    public readonly IUnitOfWork UnitOfWork;
    private readonly IServiceScope _scope;
    public readonly IOrganizationRepository OrganizationRepository;

    public DatabaseTest(IntegrationTestFixture fixture)
    {
        var dbInfo = fixture.PostgresContainer.CreateNewDatabase().Result;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbInfo.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.Migrate();

        using var dbConnection = new NpgsqlConnection(dbInfo.ConnectionString);
        dbConnection.Open();
        var respawner = Respawner.CreateAsync(dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        }).Result;
        respawner.ResetAsync(dbInfo.ConnectionString);

        _scope = fixture.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        OrganizationRepository = _scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_scope is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _scope.Dispose();
        }
    }
}
