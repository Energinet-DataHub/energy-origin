using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace API.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _testContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15.2") // Specify the PostgreSQL version you want to use
        .WithDatabase("testdb") // Set the database name
        .WithUsername("postgres") // Default username
        .WithPassword("password") // Password for the database
        .Build();
    public ApplicationDbContext? Context { get; private set; }
    public IUnitOfWork? UnitOfWork { get; private set; }

    public async Task InitializeAsync()
    {
        await _testContainer.StartAsync();
        var connectionString = _testContainer.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        Context = new ApplicationDbContext(options);
        await Context.Database.MigrateAsync(); // Apply migrations asynchronously

        UnitOfWork = new UnitOfWork(Context);
    }

    public async Task DisposeAsync()
    {
        if (UnitOfWork != null) await UnitOfWork.DisposeAsync();
        if (Context != null) await Context.DisposeAsync();
        await _testContainer.DisposeAsync();
    }
}
