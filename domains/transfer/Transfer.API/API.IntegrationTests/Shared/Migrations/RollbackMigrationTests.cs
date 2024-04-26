using System.Threading.Tasks;
using DataContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class RollbackMigrationTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public RollbackMigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.Migrate();
    }

    [Fact]
    public async Task can_rollback_all_migrations()
    {
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.MigrateAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        var rollbackAllMigrations = () => migrator.Migrate("0");
        rollbackAllMigrations.Should().NotThrow();
    }
}
