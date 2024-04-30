using System.Threading.Tasks;
using DataContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class RollbackMigrationTests : TestBase
{

    private readonly DbContextOptions<ApplicationDbContext> options;

    public RollbackMigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task can_rollback_all_migrations()
    {
        await using var dbContext = new ApplicationDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync();
        var rollbackAllMigrations = () => migrator.Migrate("0");

        rollbackAllMigrations.Should().NotThrow();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().NotBeEmpty();
    }
}
