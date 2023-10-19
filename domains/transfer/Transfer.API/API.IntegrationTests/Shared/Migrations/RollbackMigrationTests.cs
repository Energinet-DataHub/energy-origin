using System;
using System.Threading.Tasks;
using API.IntegrationTests.Shared.Testcontainers;
using API.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

public class RollbackMigrationTests : IAsyncDisposable
{
    private PostgresContainer container;
    public RollbackMigrationTests()
    {
        container = new PostgresContainer();
    }

    [Fact]
    public async Task can_rollback_all_migrations()
    {
        await container.InitializeAsync();

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(container.ConnectionString)
            .Options;
        await using var dbContext = new ApplicationDbContext(contextOptions);
        await dbContext.Database.MigrateAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        var rollbackAllMigrations = () => migrator.Migrate("0");
        rollbackAllMigrations.Should().NotThrow();
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
